using System;
using System.Collections.Generic;
using System.Linq;

namespace Rules.Model
{
    /// <summary>
    /// A model as it is explicitly defined beforehand.
    /// This is the model used for validating rules and queries.
    /// </summary>
    /// <remarks>See <see cref="DerivedModelClass"/> for models implicitly derived from rules.</remarks>
    public sealed class ExplicitModel
    {
        public Dictionary<string, ModelUnit> Units { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, ModelType> Types { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, ModelClass> Classes { get; } = new(StringComparer.Ordinal);

        public ExplicitModel()
        {
            foreach (var unit in MetaModelUnits.All) Units.Add(unit.Name, unit);
        }

        public ModelType Resolve(string id)
        {
            var parts = id.Split('.');
            if (parts.Length < 2 || !Classes.TryGetValue(parts[0], out var owner)) return null;
            for (var i = 1; i < parts.Length; i++)
            {
                if (!owner.Properties.TryGetValue(parts[i], out var property)) return null;

                if (i == parts.Length - 1)
                {
                    return property.TypeName == null ? null : property.AtomicKind == null
                        ? Types.GetValueOrDefault(property.TypeName) : new ModelType(property.TypeName, property.AtomicKind);
                }
                if (property.ClassName == null || !Classes.TryGetValue(property.ClassName, out owner)) return null;
            }
            return null;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            foreach (var type in Types.Values)
            {
                if (type.Kind == null && !Units.TryGetValue(type.BaseName, out _))
                    errors.Add($"Type '{type.Name}' refers to unknown unit '{type.BaseName}'");

                if (type.Kind == "string" && type.Values.Count != type.Values.Distinct(StringComparer.Ordinal).Count())
                    errors.Add($"Type '{type.Name}' contains duplicate string values");

                if (type.Kind == null && Units.TryGetValue(type.BaseName, out var unit))
                {
                    if (unit.Kind == "integer" && ((type.Minimum?.IsInteger == false) || (type.Maximum?.IsInteger == false)))
                    {
                        errors.Add($"Type '{type.Name}' requires integer bounds");
                    }
                    if (type.Minimum != null && type.Maximum != null &&
                        (type.Minimum.Value > type.Maximum.Value ||
                         (type.Minimum.Value == type.Maximum.Value && (!type.Minimum.Inclusive || !type.Maximum.Inclusive))))
                    {
                        errors.Add($"Type '{type.Name}' has an empty range");
                    }
                    else if (unit.Kind == "integer" && type.Minimum != null && type.Maximum != null &&
                        Math.Ceiling(type.Minimum.Value + (type.Minimum.Inclusive ? 0 : 1)) >
                        Math.Floor(type.Maximum.Value - (type.Maximum.Inclusive ? 0 : 1)))
                    {
                        errors.Add($"Type '{type.Name}' has no permitted integer values");
                    }
                }
            }
            foreach (var modelClass in Classes.Values)
            {
                foreach (var property in modelClass.Properties.Values)
                {
                    if (property.ClassName != null && !Classes.ContainsKey(property.ClassName))
                    {
                        errors.Add($"Class '{modelClass.Name}' property '{property.Name}' refers to unknown class '{property.ClassName}'");
                    }
                    if (property.ClassName == null && property.AtomicKind == null && !Types.ContainsKey(property.TypeName))
                    {
                        errors.Add($"Class '{modelClass.Name}' property '{property.Name}' refers to unknown type '{property.TypeName}'");
                    }
                }
            }
            return errors;
        }
    }

    public sealed record ModelUnit(string Name, string Kind, string ShortName);
    public sealed record ModelBound(double Value, bool Inclusive, bool IsInteger);

    public sealed class ModelType
    {
        public ModelType(string name, string baseName) { Name = name; BaseName = baseName; }
        public string Name { get; }
        public string BaseName { get; }
        public string Kind => BaseName is "string" or "boolean" or "integer" or "real" ? BaseName : null;
        public List<string> Values { get; } = [];
        public ModelBound Minimum { get; set; }
        public ModelBound Maximum { get; set; }
    }

    public sealed class ModelClass
    {
        public ModelClass(string name) { Name = name; }
        public string Name { get; }
        public Dictionary<string, ModelProperty> Properties { get; } = new(StringComparer.Ordinal);
    }

    public sealed record ModelProperty(string Name, string TypeName, string ClassName, string AtomicKind);
}
