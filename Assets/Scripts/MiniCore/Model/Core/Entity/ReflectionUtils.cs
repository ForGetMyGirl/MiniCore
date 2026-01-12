using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiniCore.Model
{
    /// <summary>
    /// 官方文档指出：设置BindingFlags的时候一定要指定Instance或static，否则Public NonPublic不会返回任何成员
    /// </summary>
    public class ReflectionUtils
    {
        /// <summary>
        /// 通过字段名获取值
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>返回一个object类型的值</returns>
        public static object GetFieldValueByName(object obj, string fieldName)
        {
            return obj.GetType().GetField(fieldName).GetValue(obj);
        }

        /// <summary>
        /// 通过字段名获取字段类型
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>返回字段类型</returns>
        public static Type GetFieldTypeByName(object obj, string fieldName)
        {
            return obj.GetType().GetField(fieldName).FieldType;
        }

        //public static Type GetFieldTypeByNameIgnoreCase(object obj, string fieldName) { 
        //    return obj.GetType().GetField()
        //}

        /// <summary>
        /// 通过属性名获取属性类型，忽略大小写
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性类型</returns>
        public static Type GetPropertyTypeByNameIgnoreCase(object obj, string propertyName)
        {
            //Type ObjType = obj.GetType();
            //PropertyInfo propertyInfo = ObjType.GetProperty(propertyName);
            //PropertyInfo prop = ObjType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).PropertyType;
        }

        /// <summary>
        /// 通过属性名获取属性信息，忽略大小写
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性信息</returns>
        public static PropertyInfo GetPropertyInfoByNameIgnoreCase(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }

        /// <summary>
        /// 通过字段名设置字段的值
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="value">要设置的值</param>
        public static void SetFieldValueByName(object obj, string fieldName, object value)
        {
            Type type = obj.GetType();
            if (type.GetField(fieldName).FieldType != value.GetType())
            {
                throw new Exception("类型不匹配！");
            }
            type.GetField(fieldName).SetValue(obj, value);
        }

        /// <summary>
        /// 通过属性名获取属性值
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        public static object GetPropertyValueByName(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName).GetValue(obj);
        }


        /// <summary>
        /// 通过属性名设置属性值,忽略大小写
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="value">值</param>
        public static void SetPropertyValueByNameIgnoreCase(object obj, string propertyName, object value)
        {
            Type type = obj.GetType();
            if (type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).PropertyType != value.GetType())
            {
                throw new Exception("类型不匹配！");
            }
            type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(obj, value);
        }


        public static List<Type> GetTypesOfMessageHandlers(Type type)
        {

            Type[] types = Assembly.GetExecutingAssembly().GetTypes();

            List<Type> results = new List<Type>();

            for (int i = 0; i < types.Length; i++)
            {
                Type curType = types[i];
                Type baseType = curType.BaseType;

                if (baseType == null || !baseType.IsGenericType) continue;

                bool isBelongs = baseType == type.MakeGenericType(baseType.GetGenericArguments()[0]);
                //bool isBaseTypeEquals = type.IsAssignableFrom(curType.BaseType);
                //bool isSubclass = curType.IsAssignableFrom(type);
                bool isAbstract = curType.IsAbstract;
                if (isBelongs && !isAbstract)
                    results.Add(curType);
            }
            return results;

        }

        public static List<Type> GetClassTypesFromCustomAssembly(Type type, string asmName)
        {

            //Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            var assmeblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = assmeblies.Where(a => a.FullName.Contains(asmName)).First();
            Type[] types = assembly.GetTypes();


            /* List<Type> results = new List<Type>();

             for (int i = 0; i < types.Length; i++)
             {
                 Type curType = types[i];
                 Type baseType = curType.BaseType;

                 if (baseType == null) continue;

                 bool isBelongs = baseType == type;
                 //bool isBaseTypeEquals = type.IsAssignableFrom(curType.BaseType);
                 //bool isSubclass = curType.IsAssignableFrom(type);
                 bool isAbstract = curType.IsAbstract;
                 if (isBelongs && !isAbstract && !curType.IsInterface)
                     results.Add(curType);
             }
             return results;*/
            return types.Where(t => type.IsAssignableFrom(t)).
                 Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass).ToList();

        }

        public static IEnumerable<Type> GetTypesOfInterface(Type type)
        {
            return type.Assembly.GetTypes()
               .Where(t => type.IsAssignableFrom(t))
               .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass);
        }

        public static void SetPropertyDefaultValue(object obj, string propertyName)
        {
            Type type = obj.GetType();
            //default(T);
            type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(obj, default);
        }
    }
}
