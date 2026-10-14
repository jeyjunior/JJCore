namespace JJCore.Domain.Attributes
{
    /// <summary>
    /// Marks a property as the primary key used by JJCore CRUD operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether the database generates the key value. Defaults to true.
        /// When false, the key is included in inserts and must be assigned by the application.
        /// </summary>
        public bool DatabaseGenerated { get; set; } = true;
    }

    /// <summary>
    /// Maps a property to a database column with a different name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// Gets the database column name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a column mapping attribute.
        /// </summary>
        /// <param name="name">The database column name.</param>
        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Excludes a property from JJCore database mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotMappedAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks the per-user sequential code property used by helper queries.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class UserCodeAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks the user identifier property used by user-owned helper queries.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class UserIdAttribute : Attribute
    {
    }

    /// <summary>
    /// Maps a class to a database table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// Gets the mapped database table name.
        /// </summary>
        public string? TableName { get; }

        /// <summary>
        /// Creates an entity mapping attribute.
        /// </summary>
        /// <param name="tableName">The database table name. When null, the class name is used.</param>
        public EntityAttribute(string? tableName = null)
        {
            TableName = tableName;
        }
    }

    /// <summary>
    /// Defines whether a mapped property can be written by insert and update operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EditableAttribute : Attribute
    {
        /// <summary>
        /// Gets whether the property is writable.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Creates an editability attribute.
        /// </summary>
        /// <param name="enabled">True when the property should be written; false to exclude it from writes.</param>
        public EditableAttribute(bool enabled)
        {
            Enabled = enabled;
        }
    }

    /// <summary>
    /// Defines the expected maximum size for a string property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StringSizeAttribute : Attribute
    {
        /// <summary>
        /// Gets the configured string length.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Creates a string size attribute.
        /// </summary>
        /// <param name="length">The expected string length.</param>
        public StringSizeAttribute(int length)
        {
            Length = length;
        }
    }

    /// <summary>
    /// Defines the expected precision and scale for a decimal property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DecimalPrecisionAttribute : Attribute
    {
        /// <summary>
        /// Gets the total number of digits.
        /// </summary>
        public int Precision { get; }

        /// <summary>
        /// Gets the number of decimal places.
        /// </summary>
        public int Scale { get; }

        /// <summary>
        /// Creates a decimal precision attribute.
        /// </summary>
        /// <param name="precision">The total number of digits.</param>
        /// <param name="scale">The number of decimal places.</param>
        public DecimalPrecisionAttribute(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }
    }

    /// <summary>
    /// Describes a relationship between a property and another table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RelationshipAttribute : Attribute
    {
        /// <summary>
        /// Gets the related table name.
        /// </summary>
        public string Table { get; }

        /// <summary>
        /// Gets the related table primary key name.
        /// </summary>
        public string PrimaryKey { get; }

        /// <summary>
        /// Creates a relationship attribute.
        /// </summary>
        /// <param name="table">The related table name.</param>
        /// <param name="primaryKey">The related table primary key name.</param>
        public RelationshipAttribute(string table, string primaryKey = "Id")
        {
            Table = table;
            PrimaryKey = primaryKey;
        }
    }

    /// <summary>
    /// Marks a mapped property as required during JJCore insert and batch insert operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RequiredFieldAttribute : Attribute
    {
    }

    /// <summary>
    /// Associates an enum field with an icon glyph and font family.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class GlyphCodeAttribute : Attribute
    {
        /// <summary>
        /// Gets the glyph value.
        /// </summary>
        public string Glyph { get; }

        /// <summary>
        /// Gets the font family used to render the glyph.
        /// </summary>
        public string FontFamily { get; }

        /// <summary>
        /// Creates a glyph code attribute.
        /// </summary>
        /// <param name="glyph">The glyph value.</param>
        /// <param name="fontFamily">The font family used to render the glyph.</param>
        public GlyphCodeAttribute(string glyph, string fontFamily)
        {
            Glyph = glyph;
            FontFamily = fontFamily;
        }
    }

    /// <summary>
    /// Associates an enum field with a font family.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class FontFamilyAttribute : Attribute
    {
        /// <summary>
        /// Gets the font family.
        /// </summary>
        public string FontFamily { get; }

        /// <summary>
        /// Creates a font family attribute.
        /// </summary>
        /// <param name="fontFamily">The font family name.</param>
        public FontFamilyAttribute(string fontFamily)
        {
            FontFamily = fontFamily;
        }
    }
}
