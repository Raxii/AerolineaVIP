using System;
using System.Collections.Generic;

namespace Con_Autenticacion.ViewModels
{
    public class EntityMetadata
    {
        public string EntityName { get; set; }
        public string DisplayName { get; set; }
        public bool IsRequired { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        public static EntityMetadata Create(string entityName, string displayName, bool isRequired = false, string description = null)
        {
            return new EntityMetadata
            {
                EntityName = entityName,
                DisplayName = displayName,
                IsRequired = isRequired,
                Description = description
            };
        }
    }
}