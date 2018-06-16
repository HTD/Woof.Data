using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Woof.DataEx {

    /// <summary>
    /// Provides conversion between SQL and CLR data types.
    /// </summary>
    public static class DataConverters {

        /// <summary>
        /// Replaces DBNull-s with null-s.
        /// </summary>
        /// <param name="x">Database object.</param>
        /// <returns>CLR safe object.</returns>
        private static object GetClrType(this object x) => x is DBNull ? null : x;

        /// <summary>
        /// Replaces DBNull-s with null-s, converts to target type.
        /// </summary>
        /// <param name="x">Database object.</param>
        /// <param name="t">CLR type object.</param>
        /// <returns>CLR safe object.</returns>
        private static object GetClrType(this object x, Type t) {
            if (x is DBNull) return null;
            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
                if (x == null) return null;
                else t = Nullable.GetUnderlyingType(t);
            }
            return Convert.ChangeType(x, t);
        }

        /// <summary>
        /// Replaces DBNull-s with null-s, converts to target type.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="x">Database object.</param>
        /// <returns>CLR safe object.</returns>
        private static T GetClrType<T>(this object x)
            => x is DBNull || x == null ? default(T) : (T)Convert.ChangeType(x, typeof(T));

        /// <summary>
        /// Reads a row from <see cref="DbDataReader"/> to the matching properties of a record.
        /// </summary>
        /// <typeparam name="T">Record type.</typeparam>
        /// <param name="reader">A <see cref="DbDataReader"/> instance.</param>
        /// <returns>Data record.</returns>
        /// <remarks>Doesn't work with structs.</remarks>
        public static T ReadToPropertiesOfNew<T>(this DbDataReader reader) where T : new() {
            if (reader == null) return default(T);
            var type = typeof(T);
            if (type.IsValueType || type.IsArray || type.IsPointer) throw new InvalidCastException();
            var record = new T();
            for (int i = 0, c = reader.FieldCount; i < c; i++) {
                var name = reader.GetName(i);
                var property = type.GetProperty(name);
                var value = reader.GetValue(i);
                if (property != null) {
                    if (property.SetMethod == null) throw new InvalidOperationException($"No setter defined for {name}.");
                    property.SetValue(record, value.GetClrType(property.PropertyType));
                }
            }
            return record;
        }

        /// <summary>
        /// Reads a row from <see cref="DbDataReader"/> to the matching fields of a record.
        /// </summary>
        /// <typeparam name="T">Record type.</typeparam>
        /// <param name="reader">A <see cref="DbDataReader"/> instance.</param>
        /// <returns>Data record.</returns>
        /// <remarks>Doesn't work with structs.</remarks>
        public static T ReadToFieldsOfNew<T>(this DbDataReader reader) where T : new() {
            var type = typeof(T);
            if (type.IsValueType || type.IsArray || type.IsPointer) throw new InvalidCastException();
            if (reader == null) return default(T);
            var record = new T();
            for (int i = 0, c = reader.FieldCount; i < c; i++) {
                var name = reader.GetName(i);
                var field = type.GetField(name);
                var value = reader.GetValue(i);
                field?.SetValue(record, value.GetClrType(field.FieldType));
            }
            return record;
        }

        /// <summary>
        /// Gets a data table from a collection of elements, the element properties will be matched with table columns.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="items">Collection of elements.</param>
        /// <returns>Data table.</returns>
        public static DataTable GetDataTableFromPropertiesOf<T>(IEnumerable<T> items) {
            var type = typeof(T);
            if (type.IsValueType || type.IsArray || type.IsPointer) throw new InvalidCastException();
            DataTable table = null;
            var properties = type.GetProperties();
            foreach (var i in items) {
                if (table == null) {
                    table = new DataTable();
                    foreach (var p in properties) table.Columns.Add(p.Name, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);
                }
                var row = table.NewRow();
                foreach (var p in properties) row[p.Name] = p.GetValue(i) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Gets a data table from a collection of elements, the element fields will be matched with table columns.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="items">Collection of elements.</param>
        /// <returns>Data table.</returns>
        public static DataTable GetDataTableFromFieldsOf<T>(IEnumerable<T> items) {
            var type = typeof(T);
            if (type.IsValueType || type.IsArray || type.IsPointer) throw new InvalidCastException();
            DataTable table = null;
            var fields = type.GetFields();
            foreach (var i in items) {
                if (table == null) {
                    table = new DataTable();
                    foreach (var p in fields) table.Columns.Add(p.Name, p.FieldType);
                }
                var row = table.NewRow();
                foreach (var p in fields) row[p.Name] = p.GetValue(i);
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Converts a data row from object array to T record.
        /// </summary>
        /// <typeparam name="T">
        /// Record type, must match exact number and order of public fields or properties from the array.
        /// </typeparam>
        /// <param name="data">Data row.</param>
        /// <returns>Data record.</returns>
        /// <remarks>Doesn't work with structs.</remarks>
        public static T As<T>(this object[] data) where T : new() {
            if (data == null) return default(T);
            var type = typeof(T);
            if (type.IsValueType || type.IsArray || type.IsPointer) throw new InvalidCastException();
            var flags = BindingFlags.Instance | BindingFlags.Public;
            var members = type.GetFields(flags) as MemberInfo[];
            var record = new T();
            if (members.Length == data.Length) { // matching fields:
                for (int i = 0, n = data.Length; i < n; i++)
                    (members[i] as FieldInfo).SetValue(record, data[i].GetClrType((members[i] as FieldInfo).FieldType));
            }
            else { // matching properties:
                members = type.GetProperties(flags);
                if (members.Length != data.Length) throw new InvalidCastException(); // ...when we failed to match fields
                for (int i = 0, n = data.Length; i < n; i++)
                    (members[i] as PropertyInfo).SetValue(record, data[i].GetClrType((members[i] as PropertyInfo).PropertyType));
            }
            return record;
        }

        /// <summary>
        /// Converts a data table from array of rows to array of T records.
        /// </summary>
        /// <typeparam name="T">Record type.</typeparam>
        /// <param name="data">Data table.</param>
        /// <returns>Array of data records.</returns>
        public static T[] AsArrayOf<T>(this object[][] data) where T : new() {
            var list = new List<T>();
            foreach (var row in data) list.Add(row.As<T>());
            return list.ToArray();
        }

        /// <summary>
        /// Convers a 2 column data table from array of rows to <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="T">Dictionary value type.</typeparam>
        /// <param name="data">2 column data table.</param>
        /// <returns>Dictionary.</returns>
        /// <remarks>
        /// The data table can have any number of columns, only the first 2 are used.
        /// </remarks>
        public static Dictionary<string, T> AsDictionary<T>(this object[][] data) {
            var dictionary = new Dictionary<string, T>();
            foreach (var row in data) dictionary.Add((string)row[0], (T)row[1]);
            return dictionary;
        }

    }

}
