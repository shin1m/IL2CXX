using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IL2CXX.Console
{
    using System;

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1) return 1;
            var assembly = Assembly.LoadFrom(args[0]);
            var entry = assembly.EntryPoint ?? throw new InvalidOperationException();
            var @out = args.Length < 2 ? "out" : args[1];
            if (Directory.Exists(@out)) Directory.Delete(@out, true);
            Directory.CreateDirectory(@out);
            //var transpiler = new Transpiler(DefaultBuiltin.Create(), Console.Error.WriteLine);
            var transpiler = new Transpiler(DefaultBuiltin.Create(), _ => { }, false);
            var names = new SortedSet<string>();
            var type2path = new Dictionary<Type, string>();
            var definition = TextWriter.Null;
            try
            {
                using var declarations = File.CreateText(Path.Combine(@out, "declarations.h"));
                using var inlines = new StringWriter();
                using var others = new StringWriter();
                using var main = File.CreateText(Path.Combine(@out, "main.cc"));
                main.WriteLine("#include \"declarations.h\"\n");
                transpiler.Do(entry, declarations, main, (type, inline) =>
                {
                    if (inline) return inlines;
                    if (type.IsInterface || type.IsSubclassOf(typeof(MulticastDelegate))) return others;
                    definition.Dispose();
                    if (type2path.TryGetValue(type, out var path)) return definition = new StreamWriter(path, true);
                    var escaped = transpiler.EscapeType(type);
                    if (escaped.Length > 240) escaped = escaped.Substring(0, 240);
                    var name = $"{escaped}.cc";
                    for (var i = 0; !names.Add(name); ++i) name = $"{escaped}__{i}.cc";
                    path = Path.Combine(@out, name);
                    type2path.Add(type, path);
                    definition = new StreamWriter(path);
                    definition.WriteLine(@"#include ""declarations.h""

namespace il2cxx
{");
                    return definition;
                }, Path.Combine(@out, "resources"));
                declarations.WriteLine("\nnamespace il2cxx\n{");
                declarations.Write(inlines);
                declarations.WriteLine("\n}");
                main.WriteLine("\nnamespace il2cxx\n{");
                main.Write(others);
                main.WriteLine("\n}");
            }
            finally
            {
                definition.Dispose();
            }
            foreach (var path in type2path.Values) File.AppendAllText(path, "\n}\n");
            void copy(string path)
            {
                var destination = Path.Combine(@out, path);
                Directory.CreateDirectory(destination);
                var source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                foreach (var x in Directory.EnumerateDirectories(source)) copy(Path.Combine(path, Path.GetFileName(x)));
                foreach (var x in Directory.EnumerateFiles(source)) File.Copy(x, Path.Combine(destination, Path.GetFileName(x)));
            }
            copy("src");
            var name = Path.GetFileNameWithoutExtension(args[0]);
            File.WriteAllText(Path.Combine(@out, "configure.ac"), $@"AC_INIT([{name}], [{DateTime.Today:yyyyMMdd}])
AM_INIT_AUTOMAKE([foreign nostdinc dist-bzip2 no-dist-gzip subdir-objects])
AC_CONFIG_SRCDIR([main.cc])

if test ""${{CXXFLAGS+set}}"" != set; then
	CXXFLAGS=
fi
AC_PROG_CXX

AC_C_INLINE
AC_TYPE_PID_T
AC_TYPE_SIZE_T

AC_ARG_ENABLE(
	[debug],
	AS_HELP_STRING([--enable-debug], [turn on debugging]),
	[case ""${{enableval}}"" in
	  yes) debug=true ;;
	  no) debug=false ;;
	  *) AC_MSG_ERROR([bad value ${{enableval}} for --enable-debug]) ;;
	 esac],
	[debug=false]
)
AM_CONDITIONAL([DEBUG], [test x$debug = xtrue])
AC_ARG_ENABLE(
	[profile],
	AS_HELP_STRING([--enable-profile], [turn on profiling]),
	[case ""${{enableval}}"" in
	  yes) profile=true ;;
	  no) profile=false ;;
	  *) AC_MSG_ERROR([bad value ${{enableval}} for --enable-profile]) ;;
	 esac],
	[profile=false]
)
AM_CONDITIONAL([PROFILE], [test x$profile = xtrue])

AC_CONFIG_HEADERS([configure.h])
AC_CONFIG_FILES([
	Makefile
])
AC_OUTPUT
");
            File.WriteAllText(Path.Combine(@out, "Makefile.am"), $@"bin_PROGRAMS = {name}
AM_CPPFLAGS = -Isrc/recyclone/include -Isrc
AM_CXXFLAGS = -std=c++17
AM_LDFLAGS =
LDADD = -lpthread -ldl
if DEBUG
AM_CXXFLAGS += -O0 -g
else
AM_CPPFLAGS += -DNDEBUG
AM_CXXFLAGS += -O3
endif
if PROFILE
AM_CXXFLAGS += -pg
AM_LDFLAGS += -pg
endif
declarations.h.gch: declarations.h
MOSTLYCLEANFILES = declarations.h.gch
{'\t'}$(CXXCOMPILE) -c -o $@ $<
GENERATEDSOURCES = \
{'\t'}{string.Join(" \\\n\t", names)} \
{'\t'}main.cc
$(GENERATEDSOURCES:.cc=.$(OBJEXT)): declarations.h.gch
{name}_SOURCES = \
{'\t'}src/types.cc \
{'\t'}src/engine.cc \
{'\t'}src/handles.cc \
{'\t'}src/waitables.cc \
{'\t'}$(GENERATEDSOURCES)
");
            return 0;
        }
    }
}
