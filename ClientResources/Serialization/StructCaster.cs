using System.Collections.Concurrent;
using System.Reflection;
using AllodsOnlineEditorTools.ClientResources.DataTypes;
using Microsoft.Extensions.Logging;

namespace AllodsOnlineEditorTools.ClientResources.Serialization;

public class StructCaster(
    IReadOnlyDictionary<string, Type> sourceStructs,
    IReadOnlyDictionary<string, Type> targetStructs,
    ILogger logger)
{
    private delegate object? ConvertFunc(object? value, ResourceSerializationContext? context);

    private readonly record struct FieldCast(StructField Source, StructField Target, ConvertFunc? Convert);

    private readonly HashSet<string> _analyzedStructs = [];
    private readonly Dictionary<string, TypeCastPlan> _plansByName = [];
    private readonly Dictionary<Type, TypeCastPlan> _plansBySourceType = [];
    private readonly Dictionary<(Type Source, Type Target), TypeCastPlan> _planCache = [];
    private readonly Dictionary<(Type DeclaringType, string FieldName), Type?> _enumRefOverrides = [];
    private readonly ConcurrentDictionary<(Type Source, Type Target, int Value), byte> _reportedEnumMisses = [];

    public int IncompatibilityCount { get; private set; }

    public IReadOnlyDictionary<(Type DeclaringType, string FieldName), Type?> EnumRefOverrides => _enumRefOverrides;

    public void Analyze(IEnumerable<string> structNames)
    {
        foreach (var name in structNames)
        {
            if (!_analyzedStructs.Add(name))
            {
                continue;
            }

            if (!sourceStructs.TryGetValue(name, out var sourceType))
            {
                continue;
            }

            if (!targetStructs.TryGetValue(name, out var targetType))
            {
                logger.LogWarning(
                    "Struct {Struct} has no implementation in the target version; its resources will be skipped", name);
                IncompatibilityCount++;
                continue;
            }

            var plan = BuildPlan(sourceType, targetType);
            _plansByName[name] = plan;
            _plansBySourceType[sourceType] = plan;
        }
    }

    public bool CanCast(string structName) => _plansByName.ContainsKey(structName);

    public object Cast(object source, ResourceSerializationContext? context = null)
    {
        if (!_plansBySourceType.TryGetValue(source.GetType(), out var plan))
        {
            throw new InvalidOperationException(
                $"No cast plan for '{source.GetType().Name}'; call {nameof(Analyze)} first and check {nameof(CanCast)}");
        }

        return CastWithPlan(source, plan, context);
    }

    private static object CastWithPlan(object source, TypeCastPlan plan, ResourceSerializationContext? context)
    {
        var result = Activator.CreateInstance(plan.TargetType)
                     ?? throw new InvalidOperationException($"Failed to create instance of '{plan.TargetType.Name}'");
        foreach (var (target, value) in plan.Defaults)
        {
            target.SetValue(result, value);
        }

        foreach (var fieldCast in plan.Fields)
        {
            var value = fieldCast.Source.GetValue(source);
            if (fieldCast.Convert is not null)
            {
                value = fieldCast.Convert(value, context);
            }

            fieldCast.Target.SetValue(result, value);
        }

        return result;
    }

    private TypeCastPlan BuildPlan(Type sourceType, Type targetType)
    {
        if (_planCache.TryGetValue((sourceType, targetType), out var cached))
        {
            return cached;
        }

        var plan = new TypeCastPlan { TargetType = targetType };
        // Register before building the fields so self-referencing struct pairs cannot recurse forever.
        _planCache[(sourceType, targetType)] = plan;

        var targetFields = StructModelCache.Get(targetType).Fields.ToDictionary(f => f.XdbName);
        var matchedTargetFields = new HashSet<string>();

        foreach (var sourceField in StructModelCache.Get(sourceType).Fields)
        {
            var xdbName = sourceField.XdbName;
            if (!targetFields.TryGetValue(xdbName, out var targetField))
            {
                logger.LogWarning("Field {Struct}.{Field} does not exist in the target version; it will be dropped",
                    sourceType.Name, sourceField.Name);
                IncompatibilityCount++;
                continue;
            }

            matchedTargetFields.Add(xdbName);

            var (supported, convert) = BuildFieldConverter(sourceField, targetField);
            if (!supported)
            {
                logger.LogWarning(
                    "Field {Struct}.{Field} cannot be cast from {SourceType} to {TargetType}; it will be dropped",
                    sourceType.Name, sourceField.Name, sourceField.FieldType.Name, targetField.FieldType.Name);
                IncompatibilityCount++;
                continue;
            }

            plan.Fields.Add(new FieldCast(sourceField, targetField, convert));
        }

        foreach (var (xdbName, targetField) in targetFields)
        {
            if (!matchedTargetFields.Contains(xdbName))
            {
                logger.LogWarning(
                    "Field {Struct}.{Field} does not exist in the source version; it will be left at its default value",
                    targetType.Name, targetField.Name);
                IncompatibilityCount++;
                if (targetField.FieldType == typeof(ResourcePointer))
                {
                    plan.Defaults.Add((targetField, ResourcePointer.Empty));
                }
            }
        }

        return plan;
    }

    private (bool Supported, ConvertFunc? Convert) BuildFieldConverter(StructField sourceField, StructField targetField)
    {
        var sourceType = sourceField.FieldType;
        var targetType = targetField.FieldType;

        if ((sourceType == typeof(int) || sourceType == typeof(int[])) && sourceType == targetType)
        {
            return BuildEnumAwareConverter(sourceField, targetField);
        }

        return BuildTypeConverter(sourceType, targetType);
    }

    private (bool Supported, ConvertFunc? Convert) BuildTypeConverter(Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
        {
            return (true, null);
        }

        if (sourceType.IsArray && targetType.IsArray)
        {
            return BuildArrayConverter(sourceType, targetType);
        }

        if (sourceType.IsClass && targetType.IsClass && sourceType.Name == targetType.Name)
        {
            return (true, BuildNestedCaster(sourceType, targetType));
        }

        return (false, null);
    }

    private (bool Supported, ConvertFunc? Convert) BuildArrayConverter(Type sourceType, Type targetType)
    {
        var sourceElement = sourceType.GetElementType()!;
        var targetElement = targetType.GetElementType()!;
        var (supported, elementConvert) = BuildTypeConverter(sourceElement, targetElement);
        if (!supported || elementConvert is null)
        {
            return (false, null);
        }

        return (true, (value, ctx) =>
                {
                    if (value is not Array sourceArray)
                    {
                        return null;
                    }

                    var targetArray = Array.CreateInstance(targetElement, sourceArray.Length);
                    for (var i = 0; i < sourceArray.Length; i++)
                    {
                        targetArray.SetValue(elementConvert(sourceArray.GetValue(i), ctx), i);
                    }

                    return targetArray;
                }
        );
    }

    private ConvertFunc BuildNestedCaster(Type sourceType, Type targetType)
    {
        var plan = BuildPlan(sourceType, targetType);
        return (value, context) => value is null ? null : CastWithPlan(value, plan, context);
    }

    private (bool Supported, ConvertFunc? Convert) BuildEnumAwareConverter(StructField sourceField,
        StructField targetField)
    {
        var sourceAttr = sourceField.Field.GetCustomAttribute<EnumRefAttribute>();
        var targetAttr = targetField.Field.GetCustomAttribute<EnumRefAttribute>();

        if (sourceAttr?.UseSourceOnCast == true || targetAttr?.UseSourceOnCast == true)
        {
            _enumRefOverrides[(targetField.DeclaringType!, targetField.Name)] = sourceAttr?.EnumType;
            if (sourceAttr is null && targetAttr is not null)
            {
                logger.LogWarning(
                    "Field {Struct}.{Field} uses the source enum on cast but the source version has none; raw numbers will be written",
                    targetField.DeclaringType!.Name, targetField.Name);
            }

            return (true, null);
        }

        var sourceEnum = sourceAttr?.EnumType;
        var targetEnum = targetAttr?.EnumType;

        if (sourceEnum is null || targetEnum is null || sourceEnum == targetEnum)
        {
            return (true, null);
        }

        var valueMap = BuildEnumValueMap(sourceEnum, targetEnum);
        if (sourceField.FieldType == typeof(int))
        {
            return (true,
                (value, _) =>
                    value is int intValue ? RemapEnumValue(intValue, sourceEnum, targetEnum, valueMap) : value);
        }

        return (true, (value, _) =>
                {
                    if (value is not int[] values)
                    {
                        return value;
                    }

                    var remapped = new int[values.Length];
                    for (var i = 0; i < values.Length; i++)
                    {
                        remapped[i] = RemapEnumValue(values[i], sourceEnum, targetEnum, valueMap);
                    }

                    return remapped;
                }
        );
    }

    private static Dictionary<int, int> BuildEnumValueMap(Type sourceEnum, Type targetEnum)
    {
        var map = new Dictionary<int, int>();
        foreach (var name in Enum.GetNames(sourceEnum))
        {
            if (Enum.TryParse(targetEnum, name, out var targetValue))
            {
                map[Convert.ToInt32(Enum.Parse(sourceEnum, name))] = Convert.ToInt32(targetValue);
            }
        }

        return map;
    }

    private int RemapEnumValue(int value, Type sourceEnum, Type targetEnum, Dictionary<int, int> valueMap)
    {
        if (valueMap.TryGetValue(value, out var mapped))
        {
            return mapped;
        }

        if (_reportedEnumMisses.TryAdd((sourceEnum, targetEnum, value), 0))
        {
            logger.LogWarning(
                "Enum value {Value} ({Name}) of {SourceEnum} has no counterpart in {TargetEnum}; the numeric value is kept",
                value, Enum.GetName(sourceEnum, value) ?? "unnamed", sourceEnum.Name, targetEnum.Name);
        }

        return value;
    }

    private sealed class TypeCastPlan
    {
        public required Type TargetType { get; init; }
        public List<FieldCast> Fields { get; } = [];
        public List<(StructField Target, object Value)> Defaults { get; } = [];
    }
}
