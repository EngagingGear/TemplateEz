using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TemplateEzNS;

namespace TemplateEzTest
{
    public class BaseTest
    {
        private readonly Dictionary<string, TemplateEz> _cache = new Dictionary<string, TemplateEz>();

        public string[] SubSpecialChars(params string[] array)
        {
            return array.Select(s => s
                                .Replace("^", "\\")
                                .Replace("|", "\"")).ToArray();
        }

        public TemplateEz GenerateEzTemplate(string[] s, string type, bool noWrapInOutputAdd = false)
        {
            lock ( _cache)
            {
                // Get a unique identifier for requested template
                string combinedName = s + type;
                // Look up in cache, if not there generate it and return
                if (!_cache.ContainsKey(combinedName))
                    _cache[combinedName] = new TemplateEz(s, type, noWrapInOutputAdd: noWrapInOutputAdd);
                return _cache[combinedName];
            }
        }

        public bool CompareStrList(List<string> l1, params string[] compareTo)
        {
            return CompareStrList(l1, compareTo.ToList());
        }

        public bool CompareStrList(List<string> list1, List<string> list2)
        {
            if (list1.Count != list2.Count)
                return false;
            for (var i = 0; i < list1.Count; i++)
                if (list1[i] != list2[i])
                    return false;
            return true;
        }

        public bool CompareList<T>(Func<T, T, bool> comp, List<T> l1, params T[] compareTo)
        {
            return CompareList(comp, l1, compareTo.ToList());
        }

        public bool CompareList<T>(Func<T, T, bool> comp, List<T> list1, List<T> list2)
        {
            if (list1.Count != list2.Count)
                return false;
            for (var i = 0; i < list1.Count; i++)
                if (!comp(list1[i], list2[i]))
                    return false;
            return true;
        }

        public object CallPrivateMethod(object obj, string name, params object[] p)
        {
            Type t = obj.GetType();
            var methodInfos = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            var method = methodInfos.First(m => m.Name == name);
            return method.Invoke(obj, p);
        }

        protected static string LibraryPath()
        {
            var cwd = Directory.GetCurrentDirectory();
            var libraryPath = Path.Combine(cwd, "../../../../LibraryForTest/bin/Debug/net5.0/LibraryForTest.Dll");
            return libraryPath;
        }
        protected static string InvalidLibraryPath()
        {
            var cwd = Directory.GetCurrentDirectory();
            var libraryPath = Path.Combine(cwd, "../../../../LibraryForTest/bin/Debug/net5.0/Invalid.Dll");
            return libraryPath;
        }
    }
}
