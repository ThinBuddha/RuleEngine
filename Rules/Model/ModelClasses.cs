using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Rules.Rules;

namespace Rules.Model
{
    /// <summary>A node in the hierarchy formed by dotted fact IDs.</summary>
    public class ModelClass
    {
        private readonly SortedDictionary<string, ModelClass> _classes = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, ModelProperty> _properties = new(StringComparer.Ordinal);
        private readonly IReadOnlyDictionary<string, ModelClass> _classesView;
        private readonly IReadOnlyDictionary<string, ModelProperty> _propertiesView;

        internal ModelClass(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
            _classesView = new ReadOnlyDictionary<string, ModelClass>(_classes);
            _propertiesView = new ReadOnlyDictionary<string, ModelProperty>(_properties);
        }

        /// <summary>The final segment of this class's dotted path. Empty for the root.</summary>
        public string Name { get; }
        /// <summary>The dotted path from the root. Empty for the root.</summary>
        public string FullName { get; }
        public IReadOnlyDictionary<string, ModelClass> Classes => _classesView;
        public IReadOnlyDictionary<string, ModelProperty> Properties => _propertiesView;

        internal ModelClass GetOrAddClass(string name)
        {
            if (!_classes.TryGetValue(name, out var modelClass))
            {
                var fullName = FullName.Length == 0 ? name : $"{FullName}.{name}";
                _classes.Add(name, modelClass = new ModelClass(name, fullName));
            }
            return modelClass;
        }

        internal void AddProperty(ModelProperty property) => _properties.Add(property.Name, property);
    }

    /// <summary>A fact identified by a rule condition or conclusion.</summary>
    public class ModelProperty
    {
        internal ModelProperty(string name, string fullName, string kind, IReadOnlyList<string> stringValues)
        {
            Name = name;
            FullName = fullName;
            Kind = kind;
            StringValues = Array.AsReadOnly(stringValues.ToArray());
        }

        public string Name { get; }
        public string FullName { get; }
        /// <summary>"Boolean", "numeric", "string", or null if unconstrained.</summary>
        public string Kind { get; }
        public IReadOnlyList<string> StringValues { get; }
    }

    public class ModelDeriver
    {
        /// <summary>Builds a tree from the fact IDs and types inferred from the rules.</summary>
        public static ModelClass Derive(RuleContainers containers)
        {
            ArgumentNullException.ThrowIfNull(containers);
            return Derive(DerivedModelTypes.Derive(containers));
        }

        /// <summary>Builds a tree from an existing type derivation.</summary>
        public static ModelClass Derive(DerivedModelTypes types)
        {
            ArgumentNullException.ThrowIfNull(types);
            var root = new ModelClass(string.Empty, string.Empty);

            foreach (var id in types.Identifiers.OrderBy(id => id, StringComparer.Ordinal))
            {
                var parts = id.Split('.');
                var owner = root;
                for (var i = 0; i < parts.Length - 1; i++)
                    owner = owner.GetOrAddClass(parts[i]);

                owner.AddProperty(new ModelProperty(parts[^1], id,
                    types.GetKind(id), types.GetStringValues(id)));
            }

            return root;
        }
    }
}
