#ifndef __MONO_CONFIG_H__
#define __MONO_CONFIG_H__

#ifdef _MSC_VER

// FIXME This is all questionable but the logs are flooded and nothing else is fixing them.
#pragma warning(disable:4018) // signed/unsigned mismatch
#pragma warning(disable:4090) // const problem
#pragma warning(disable:4146) // unary minus operator applied to unsigned type, result still unsigned
#pragma warning(disable:4244) // integer conversion, possible loss of data
#pragma warning(disable:4267) // integer conversion, possible loss of data

// promote warnings to errors
#pragma warning(  error:4013) // function undefined; assuming extern returning int
#pragma warning(  error:4022) // call and prototype disagree
#pragma warning(  error:4047) // differs in level of indirection
#pragma warning(  error:4098) // void return returns a value
#pragma warning(  error:4113) // call and prototype disagree
#pragma warning(  error:4172) // returning address of local variable or temporary
#pragma warning(  error:4197) // top-level volatile in cast is ignored
#pragma warning(  error:4273) // inconsistent dll linkage
#pragma warning(  error:4293) // shift count negative or too big, undefined behavior
#pragma warning(  error:4312) // 'type cast': conversion from 'MonoNativeThreadId' to 'gpointer' of greater size
#pragma warning(  error:4715) // 'keyword' not all control paths return a value

#include <SDKDDKVer.h>

#if _WIN32_WINNT < 0x0601
#error "Mono requires Windows 7 or later."
#endif /* _WIN32_WINNT < 0x0601 */

#ifndef HAVE_WINAPI_FAMILY_SUPPORT

#define HAVE_WINAPI_FAMILY_SUPPORT

/* WIN API Family support */
#include <winapifamily.h>

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
	#define HAVE_CLASSIC_WINAPI_SUPPORT 1
	#define HAVE_UWP_WINAPI_SUPPORT 0
#elif WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_APP)
	#define HAVE_CLASSIC_WINAPI_SUPPORT 0
	#define HAVE_UWP_WINAPI_SUPPORT 1
#else
	#define HAVE_CLASSIC_WINAPI_SUPPORT 0
	#define HAVE_UWP_WINAPI_SUPPORT 0
#ifndef HAVE_EXTERN_DEFINED_WINAPI_SUPPORT
	#error Unsupported WINAPI family
#endif
#endif

#endif
#endif

/* Define to the full name of this package. */
/* #undef PACKAGE_NAME */

/* Define to the one symbol short name of this package. */
/* #undef PACKAGE_TARNAME */

/* Define to the version of this package. */
/* #undef PACKAGE_VERSION */

/* Define to the full name and version of this package. */
/* #undef PACKAGE_STRING */

/* Define to the address where bug reports for this package should be sent. */
/* #undef PACKAGE_BUGREPORT */

/* Define to the home page for this package. */
/* #undef PACKAGE_URL */

/* This platform does not support symlinks */
/* #undef HOST_NO_SYMLINKS */

/* pthread is a pointer */
/* #undef PTHREAD_POINTER_ID */

/* Targeting the Android platform */
/* #undef HOST_ANDROID */

/* ... */
/* #undef TARGET_ANDROID */

/* ... */
/* #undef USE_MACH_SEMA */

/* Targeting the Fuchsia platform */
/* #undef HOST_FUCHSIA */

/* Targeting the AIX and PASE platforms */
/* #undef HOST_AIX */

/* Host Platform is Win32 */
/* #undef HOST_WIN32 */

/* Target Platform is Win32 */
/* #undef TARGET_WIN32 */

/* Host Platform is Darwin */
/* #undef HOST_DARWIN */

/* Host Platform is iOS */
/* #undef HOST_IOS */

/* Host Platform is tvOS */
/* #undef HOST_TVOS */

/* Host Platform is Mac Catalyst */
/* #undef HOST_MACCAT */

/* Use classic Windows API support */
#define HAVE_CLASSIC_WINAPI_SUPPORT 1

/* Don't use UWP Windows API support */
/* #undef HAVE_UWP_WINAPI_SUPPORT */

/* Define to 1 if you have the ANSI C header files. */
/* #undef STDC_HEADERS */

/* Define to 1 if you have the <sys/types.h> header file. */
#define HAVE_SYS_TYPES_H 1

/* Define to 1 if you have the <sys/stat.h> header file. */
#define HAVE_SYS_STAT_H 1

/* Define to 1 if you have the <strings.h> header file. */
#define HAVE_STRINGS_H 1

/* Define to 1 if you have the <stdint.h> header file. */
#define HAVE_STDINT_H 1

/* Define to 1 if you have the <unistd.h> header file. */
#define HAVE_UNISTD_H 1

/* Define to 1 if you have the <signal.h> header file. */
#define HAVE_SIGNAL_H 1

/* Define to 1 if you have the <setjmp.h> header file. */
#define HAVE_SETJMP_H 1

/* Define to 1 if you have the <syslog.h> header file. */
#define HAVE_SYSLOG_H 1

/* Define to 1 if `major', `minor', and `makedev' are declared in <mkdev.h>.
   */
/* #undef MAJOR_IN_MKDEV */

/* Define to 1 if `major', `minor', and `makedev' are declared in
   <sysmacros.h>. */
/* #undef MAJOR_IN_SYSMACROS */

/* Define to 1 if you have the <sys/filio.h> header file. */
/* #undef HAVE_SYS_FILIO_H */

/* Define to 1 if you have the <sys/sockio.h> header file. */
/* #undef HAVE_SYS_SOCKIO_H */

/* Define to 1 if you have the <netdb.h> header file. */
#define HAVE_NETDB_H 1

/* Define to 1 if you have the <utime.h> header file. */
#define HAVE_UTIME_H 1

/* Define to 1 if you have the <sys/utime.h> header file. */
/* #undef HAVE_SYS_UTIME_H */

/* Define to 1 if you have the <semaphore.h> header file. */
#define HAVE_SEMAPHORE_H 1

/* Define to 1 if you have the <sys/un.h> header file. */
#define HAVE_SYS_UN_H 1

/* Define to 1 if you have the <sys/syscall.h> header file. */
#define HAVE_SYS_SYSCALL_H 1

/* Define to 1 if you have the <sys/uio.h> header file. */
#define HAVE_SYS_UIO_H 1

/* Define to 1 if you have the <sys/param.h> header file. */
#define HAVE_SYS_PARAM_H 1

/* Define to 1 if you have the <sys/sysctl.h> header file. */
#define HAVE_SYS_SYSCTL_H 1

/* Define to 1 if you have the <libproc.h> header file. */
/* #undef HAVE_LIBPROC_H */

/* Define to 1 if you have the <sys/prctl.h> header file. */
#define HAVE_SYS_PRCTL_H 1

/* Define to 1 if you have the <gnu/lib-names.h> header file. */
/* #undef HAVE_GNU_LIB_NAMES_H */

/* Define to 1 if you have the <sys/socket.h> header file. */
#define HAVE_SYS_SOCKET_H 1

/* Define to 1 if you have the <sys/utsname.h> header file. */
#define HAVE_SYS_UTSNAME_H 1

/* Define to 1 if you have the <alloca.h> header file. */
#define HAVE_ALLOCA_H 1

/* Define to 1 if you have the <ucontext.h> header file. */
#define HAVE_UCONTEXT_H 1

/* Define to 1 if you have the <pwd.h> header file. */
#define HAVE_PWD_H 1

/* Define to 1 if you have the <sys/select.h> header file. */
#define HAVE_SYS_SELECT_H 1

/* Define to 1 if you have the <netinet/tcp.h> header file. */
#define HAVE_NETINET_TCP_H 1

/* Define to 1 if you have the <netinet/in.h> header file. */
#define HAVE_NETINET_IN_H 1

/* Define to 1 if you have the <link.h> header file. */
#define HAVE_LINK_H 1

/* Define to 1 if you have the <arpa/inet.h> header file. */
#define HAVE_ARPA_INET_H 1

/* Define to 1 if you have the <unwind.h> header file. */
#define HAVE_UNWIND_H 1

/* Define to 1 if you have the <sys/user.h> header file. */
#define HAVE_SYS_USER_H 1

/* Use static ICU */
#define STATIC_ICU 1

/* Use OS-provided zlib */
/* #undef HAVE_SYS_ZLIB */

/* Define to 1 if you have the <poll.h> header file. */
#define HAVE_POLL_H 1

/* Define to 1 if you have the <sys/poll.h> header file. */
#define HAVE_SYS_POLL_H 1

/* Define to 1 if you have the <sys/wait.h> header file. */
#define HAVE_SYS_WAIT_H 1

/* Define to 1 if you have the <wchar.h> header file. */
#define HAVE_WCHAR_H 1

/* Define to 1 if you have the <linux/magic.h> header file. */
/* #undef HAVE_LINUX_MAGIC_H */

/* Define to 1 if you have the <android/legacy_signal_inlines.h> header file.
   */
/* #undef HAVE_ANDROID_LEGACY_SIGNAL_INLINES_H */

/* Define to 1 if you have the <android/ndk-version.h> header file. */
/* #undef HAVE_ANDROID_NDK_VERSION_H */

/* Whether Android NDK unified headers are used */
/* #undef ANDROID_UNIFIED_HEADERS */

/* The size of `void *', as computed by sizeof. */
#define SIZEOF_VOID_P 4

/* The size of `long', as computed by sizeof. */
#define SIZEOF_LONG 4

/* The size of `int', as computed by sizeof. */
#define SIZEOF_INT 4

/* The size of `long long', as computed by sizeof. */
#define SIZEOF_LONG_LONG 8

/* Xen-specific behaviour */
/* #undef MONO_XEN_OPT */

/* Reduce runtime requirements (and capabilities) */
/* #undef MONO_SMALL_CONFIG */

/* Make jemalloc assert for mono */
/* #undef MONO_JEMALLOC_ASSERT */

/* Make jemalloc default for mono */
/* #undef MONO_JEMALLOC_DEFAULT */

/* Enable jemalloc usage for mono */
/* #undef MONO_JEMALLOC_ENABLED */

/* Do not include names of unmanaged functions in the crash dump */
/* #undef MONO_PRIVATE_CRASHES */

/* Do not create structured crash files during unmanaged crashes */
/* #undef DISABLE_STRUCTURED_CRASH */

/* String of disabled features */
#define DISABLED_FEATURES ""

/* Disable AOT Compiler */
/* #undef DISABLE_AOT */

/* Disable runtime debugging support */
/* #undef DISABLE_DEBUG */

/* Disable reflection emit support */
/* #undef DISABLE_REFLECTION_EMIT */

/* Disable support debug logging */
#define DISABLE_LOGGING 1

/* Disable COM support */
#define DISABLE_COM 1

/* Disable advanced SSA JIT optimizations */
/* #undef DISABLE_SSA */

/* Disable the JIT, only full-aot mode or interpreter will be supported by the
   runtime. */
#define DISABLE_JIT 1

/* Disable the interpreter. */
#define DISABLE_INTERPRETER 1

/* Some VES is available at runtime */
/* #undef ENABLE_ILGEN */

/* Disable non-blittable marshalling */
/* #undef DISABLE_NONBLITTABLE */

/* Disable SIMD intrinsics related optimizations. */
/* #undef DISABLE_SIMD */

/* Disable Soft Debugger Agent. */
#define DISABLE_DEBUGGER_AGENT 1

/* Disable Performance Counters. */
#define DISABLE_PERFCOUNTERS 1

/* Disable shared perfcounters. */
#define DISABLE_SHARED_PERFCOUNTERS 1

/* Disable support code for the LLDB plugin. */
/* #undef DISABLE_LLDB */

/* Disable support for .mdb symbol files. */
#define DISABLE_MDB 1

/* Disable assertion messages. */
#define DISABLE_ASSERT_MESSAGES 1

/* Disable concurrent gc support in SGEN. */
#define DISABLE_SGEN_MAJOR_MARKSWEEP_CONC 1

/* Disable minor=split support in SGEN. */
#define DISABLE_SGEN_SPLIT_NURSERY 1

/* Disable gc bridge support in SGEN. */
#define DISABLE_SGEN_GC_BRIDGE 1

/* Disable debug helpers in SGEN. */
#define DISABLE_SGEN_DEBUG_HELPERS 1

/* Disable sockets */
/* #undef DISABLE_SOCKETS */

/* Disables use of DllMaps in MonoVM */
#define DISABLE_DLLMAP 1

/* Disable Threads */
#define DISABLE_THREADS 1

/* Disable perf counters */
/* #undef DISABLE_PERF_COUNTERS */

/* Disable MONO_LOG_DEST */
#define DISABLE_LOG_DEST

/* GC description */
/* #undef DEFAULT_GC_NAME */

/* No GC support. */
/* #undef HAVE_NULL_GC */

/* Length of zero length arrays */
#define MONO_ZERO_LEN_ARRAY 0

/* Define to 1 if you have the `sigaction' function. */
#define HAVE_SIGACTION 1

/* Define to 1 if you have the `kill' function. */
#define HAVE_KILL 1

/* CLOCK_MONOTONIC */
#define HAVE_CLOCK_MONOTONIC 1

/* CLOCK_MONOTONIC_COARSE */
#define HAVE_CLOCK_MONOTONIC_COARSE 1

/* clockid_t */
#define HAVE_CLOCKID_T 1

/* mach_absolute_time */
/* #undef HAVE_MACH_ABSOLUTE_TIME */

/* gethrtime */
/* #undef HAVE_GETHRTIME */

/* read_real_time */
/* #undef HAVE_READ_REAL_TIME */

/* Define to 1 if you have the `clock_nanosleep' function. */
/* #undef HAVE_CLOCK_NANOSLEEP */

/* Does dlsym require leading underscore. */
/* #undef MONO_DL_NEED_USCORE */

/* Define to 1 if you have the <execinfo.h> header file. */
/* #undef HAVE_EXECINFO_H */

/* Define to 1 if you have the <sys/auxv.h> header file. */
/* #undef HAVE_SYS_AUXV_H */

/* Define to 1 if you have the <sys/resource.h> header file. */
#define HAVE_SYS_RESOURCE_H 1

/* kqueue */
/* #undef HAVE_KQUEUE */

/* Define to 1 if you have the `backtrace_symbols' function. */
/* #undef HAVE_BACKTRACE_SYMBOLS */

/* Define to 1 if you have the `mkstemp' function. */
#define HAVE_MKSTEMP 1

/* Define to 1 if you have the `mmap' function. */
#define HAVE_MMAP 1

/* Define to 1 if you have the `madvise' function. */
#define HAVE_MADVISE 1

/* Define to 1 if you have the `getrusage' function. */
#define HAVE_GETRUSAGE 1

/* Define to 1 if you have the `dladdr' function. */
#define HAVE_DLADDR 1

/* Define to 1 if you have the `sysconf' function. */
#define HAVE_SYSCONF 1

/* Define to 1 if you have the `getrlimit' function. */
#define HAVE_GETRLIMIT 1

/* Define to 1 if you have the `prctl' function. */
/* #undef HAVE_PRCTL */

/* Define to 1 if you have the `nl_langinfo' function. */
#define HAVE_NL_LANGINFO 1

/* sched_getaffinity */
/* #undef HAVE_SCHED_GETAFFINITY */

/* sched_setaffinity */
/* #undef HAVE_SCHED_SETAFFINITY */

/* Define to 1 if you have the `sched_getcpu' function. */
/* #undef HAVE_SCHED_GETCPU */

/* Define to 1 if you have the `getpwuid_r' function. */
#define HAVE_GETPWUID_R 1

/* Define to 1 if you have the `readlink' function. */
#define HAVE_READLINK 1

/* Define to 1 if you have the `chmod' function. */
#define HAVE_CHMOD 1

/* Define to 1 if you have the `lstat' function. */
#define HAVE_LSTAT 1

/* Define to 1 if you have the `getdtablesize' function. */
/* #undef HAVE_GETDTABLESIZE */

/* Define to 1 if you have the `ftruncate' function. */
#define HAVE_FTRUNCATE 1

/* Define to 1 if you have the `msync' function. */
#define HAVE_MSYNC 1

/* Define to 1 if you have the `getpeername' function. */
#define HAVE_GETPEERNAME 1

/* Define to 1 if you have the `utime' function. */
#define HAVE_UTIME 1

/* Define to 1 if you have the `utimes' function. */
#define HAVE_UTIMES 1

/* Define to 1 if you have the `openlog' function. */
#define HAVE_OPENLOG 1

/* Define to 1 if you have the `closelog' function. */
#define HAVE_CLOSELOG 1

/* Define to 1 if you have the `atexit' function. */
#define HAVE_ATEXIT 1

/* Define to 1 if you have the `popen' function. */
/* #undef HAVE_POPEN */

/* Define to 1 if you have the `strerror_r' function. */
#define HAVE_STRERROR_R 1

/* Have GLIBC_BEFORE_2_3_4_SCHED_SETAFFINITY */
/* #undef GLIBC_BEFORE_2_3_4_SCHED_SETAFFINITY */

/* GLIBC has CPU_COUNT macro in sched.h */
/* #undef HAVE_GNU_CPU_COUNT */

/* Have large file support */
/* #undef HAVE_LARGE_FILE_SUPPORT */

/* Have getaddrinfo */
#define HAVE_GETADDRINFO 1

/* Have gethostbyname2 */
/* #undef HAVE_GETHOSTBYNAME2 */

/* Have gethostbyname */
#define HAVE_GETHOSTBYNAME 1

/* Have getprotobyname */
#define HAVE_GETPROTOBYNAME 1

/* Have getprotobyname_r */
/* #undef HAVE_GETPROTOBYNAME_R */

/* Have getnameinfo */
#define HAVE_GETNAMEINFO 1

/* Have inet_ntop */
#define HAVE_INET_NTOP 1

/* Have inet_pton */
#define HAVE_INET_PTON 1

/* Define to 1 if you have the `inet_aton' function. */
#define HAVE_INET_ATON 1

/* Define to 1 if you have the <pthread.h> header file. */
#define HAVE_PTHREAD_H 1

/* Define to 1 if you have the <pthread_np.h> header file. */
/* #undef HAVE_PTHREAD_NP_H */

/* Define to 1 if you have the `pthread_mutex_timedlock' function. */
/* #undef HAVE_PTHREAD_MUTEX_TIMEDLOCK */

/* Define to 1 if you have the `pthread_getattr_np' function. */
/* #undef HAVE_PTHREAD_GETATTR_NP */

/* Define to 1 if you have the `pthread_attr_get_np' function. */
/* #undef HAVE_PTHREAD_ATTR_GET_NP */

/* Define to 1 if you have the `pthread_getname_np' function. */
/* #undef HAVE_PTHREAD_GETNAME_NP */

/* Define to 1 if you have the `pthread_setname_np' function. */
/* #undef HAVE_PTHREAD_SETNAME_NP */

/* Define to 1 if you have the `pthread_cond_timedwait_relative_np' function.
   */
/* #undef HAVE_PTHREAD_COND_TIMEDWAIT_RELATIVE_NP */

/* Define to 1 if you have the `pthread_kill' function. */
/* #undef HAVE_PTHREAD_KILL */

/* Define to 1 if you have the `pthread_attr_setstacksize' function. */
#define HAVE_PTHREAD_ATTR_SETSTACKSIZE 1

/* Define to 1 if you have the `pthread_attr_getstack' function. */
/* #undef HAVE_PTHREAD_ATTR_GETSTACK */

/* Define to 1 if you have the `pthread_attr_getstacksize' function. */
/* #undef HAVE_PTHREAD_ATTR_GETSTACKSIZE */

/* Define to 1 if you have the `pthread_get_stacksize_np' function. */
/* #undef HAVE_PTHREAD_GET_STACKSIZE_NP */

/* Define to 1 if you have the `pthread_get_stackaddr_np' function. */
/* #undef HAVE_PTHREAD_GET_STACKADDR_NP */

/* Define to 1 if you have the `pthread_jit_write_protect_np' function. */
/* #undef HAVE_PTHREAD_JIT_WRITE_PROTECT_NP */

#define HAVE_GETAUXVAL 1

/* Define to 1 if you have the declaration of `pthread_mutexattr_setprotocol',
   and to 0 if you don't. */
#define HAVE_DECL_PTHREAD_MUTEXATTR_SETPROTOCOL 1

/* Have a working sigaltstack */
/* #undef HAVE_WORKING_SIGALTSTACK */

/* Define to 1 if you have the `shm_open' function. */
#define HAVE_SHM_OPEN 1

/* Define to 1 if you have the `poll' function. */
#define HAVE_POLL 1

/* epoll_create1 */
/* #undef HAVE_EPOLL */

/* Define to 1 if you have the <sys/ioctl.h> header file. */
#define HAVE_SYS_IOCTL_H 1

/* Define to 1 if you have the <net/if.h> header file. */
#define HAVE_NET_IF_H 1

/* Can get interface list */
/* #undef HAVE_SIOCGIFCONF */

/* sockaddr_in has sin_len */
/* #undef HAVE_SOCKADDR_IN_SIN_LEN */

/* sockaddr_in6 has sin6_len */
/* #undef HAVE_SOCKADDR_IN6_SIN_LEN */

/* Have getifaddrs */
#define HAVE_GETIFADDRS 1

/* Have access */
#define HAVE_ACCESS 1

/* Have getpid */
#define HAVE_GETPID 1

/* Have mktemp */
#define HAVE_MKTEMP 1

/* Define to 1 if you have the <sys/errno.h> header file. */
/* #undef HAVE_SYS_ERRNO_H */

/* Define to 1 if you have the <sys/sendfile.h> header file. */
/* #undef HAVE_SYS_SENDFILE_H */

/* Define to 1 if you have the <sys/statvfs.h> header file. */
#define HAVE_SYS_STATVFS_H 1

/* Define to 1 if you have the <sys/statfs.h> header file. */
#define HAVE_SYS_STATFS_H 1

/* Define to 1 if you have the <sys/mman.h> header file. */
#define HAVE_SYS_MMAN_H 1

/* Define to 1 if you have the <sys/mount.h> header file. */
#define HAVE_SYS_MOUNT_H 1

/* Define to 1 if you have the `getfsstat' function. */
/* #undef HAVE_GETFSSTAT */

/* Define to 1 if you have the `mremap' function. */
#define HAVE_MREMAP 1

/* Define to 1 if you have the `posix_fadvise' function. */
#define HAVE_POSIX_FADVISE 1

/* Define to 1 if you have the `vsnprintf' function. */
#define HAVE_VSNPRINTF 1

/* Define to 1 if you have the `sendfile' function. */
/* #undef HAVE_SENDFILE */

/* struct statfs */
#define HAVE_STATFS 1

/* Define to 1 if you have the `statvfs' function. */
#define HAVE_STATVFS 1

/* Define to 1 if you have the `setpgid' function. */
#define HAVE_SETPGID 1

/* Define to 1 if you have the `system' function. */
#ifdef _MSC_VER
#if HAVE_WINAPI_FAMILY_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#define HAVE_SYSTEM 1
#endif
#else
#define HAVE_SYSTEM 1
#endif

/* Define to 1 if you have the `fork' function. */
/* #undef HAVE_FORK */

/* Define to 1 if you have the `execv' function. */
#define HAVE_EXECV 1

/* Define to 1 if you have the `execve' function. */
#define HAVE_EXECVE 1

/* Define to 1 if you have the `waitpid' function. */
#define HAVE_WAITPID 1

/* Define to 1 if you have the `localtime_r' function. */
#define HAVE_LOCALTIME_R 1

/* Define to 1 if you have the `mkdtemp' function. */
#define HAVE_MKDTEMP 1

/* The size of `size_t', as computed by sizeof. */
#define SIZEOF_SIZE_T 4

#define HAVE_GNU_STRERROR_R 0

/* Define to 1 if the system has the type `struct sockaddr'. */
/* #undef HAVE_STRUCT_SOCKADDR */

/* Define to 1 if the system has the type `struct sockaddr_in'. */
/* #undef HAVE_STRUCT_SOCKADDR_IN */

/* Define to 1 if the system has the type `struct sockaddr_in6'. */
#define HAVE_STRUCT_SOCKADDR_IN6 1

/* Define to 1 if the system has the type `struct stat'. */
/* #undef HAVE_STRUCT_STAT */

/* Define to 1 if the system has the type `struct timeval'. */
#define HAVE_STRUCT_TIMEVAL 1

/* Define to 1 if `st_atim' is a member of `struct stat'. */
#define HAVE_STRUCT_STAT_ST_ATIM 1

/* Define to 1 if `st_atimespec' is a member of `struct stat'. */
/* #undef HAVE_STRUCT_STAT_ST_ATIMESPEC */

/* Define to 1 if `kp_proc' is a member of `struct kinfo_proc'. */
/* #undef HAVE_STRUCT_KINFO_PROC_KP_PROC */

/* Define to 1 if you have the <sys/time.h> header file. */
#define HAVE_SYS_TIME_H 1

/* Define to 1 if you have the <dirent.h> header file. */
#define HAVE_DIRENT_H 1

/* Define to 1 if you have the <CommonCrypto/CommonDigest.h> header file. */
/* #undef HAVE_COMMONCRYPTO_COMMONDIGEST_H */

/* Define to 1 if you have the <sys/random.h> header file. */
#define HAVE_SYS_RANDOM_H 1

/* Define to 1 if you have the `getrandom' function. */
/* #undef HAVE_GETRANDOM */

/* Define to 1 if you have the `getentropy' function. */
#define HAVE_GETENTROPY 1

/* Qp2getifaddrs */
/* #undef HAVE_QP2GETIFADDRS */

/* Define to 1 if you have the `strlcpy' function. */
#define HAVE_STRLCPY 1

/* Define to 1 if you have the <winternl.h> header file. */
/* #undef HAVE_WINTERNL_H */

/* Have socklen_t */
#define HAVE_SOCKLEN_T 1

/* Define to 1 if you have the `execvp' function. */
#define HAVE_EXECVP 1

/* Name of /dev/random */
#define NAME_DEV_RANDOM "/dev/random"

/* Enable the allocation and indexing of arrays greater than Int32.MaxValue */
/* #undef MONO_BIG_ARRAYS */

/* Enable DTrace probes */
/* #undef ENABLE_DTRACE */

/* AOT cross offsets file */
/* #undef MONO_OFFSETS_FILE */

/* Enable the LLVM back end */
/* #undef ENABLE_LLVM */

/* Runtime support code for llvm enabled */
#define ENABLE_LLVM_RUNTIME 1

/* 64 bit mode with 4 byte longs and pointers */
/* #undef MONO_ARCH_ILP32 */

/* The runtime is compiled for cross-compiling mode */
/* #undef MONO_CROSS_COMPILE */

/* ... */
#define TARGET_WASM 1

/* The JIT/AOT targets WatchOS */
/* #undef TARGET_WATCHOS */

/* ... */
/* #undef TARGET_PS3 */

/* ... */
/* #undef __mono_ppc64__ */

/* ... */
/* #undef TARGET_XBOX360 */

/* ... */
/* #undef TARGET_PS4 */

/* ... */
/* #undef DISABLE_HW_TRAPS */

/* Target is RISC-V */
/* #undef TARGET_RISCV */

/* Target is 32-bit RISC-V */
/* #undef TARGET_RISCV32 */

/* Target is 64-bit RISC-V */
/* #undef TARGET_RISCV64 */

/* ... */
/* #undef TARGET_X86 */

/* ... */
/* #undef TARGET_AMD64 */

/* ... */
/* #undef TARGET_ARM */

/* ... */
/* #undef TARGET_ARM64 */

/* ... */
/* #undef TARGET_POWERPC */

/* ... */
/* #undef TARGET_POWERPC64 */

/* ... */
/* #undef TARGET_S390X */

/* ... */
/* #undef TARGET_MIPS */

/* ... */
/* #undef TARGET_SPARC */

/* ... */
/* #undef TARGET_SPARC64 */

/* ... */
#define HOST_WASM 1

/* ... */
#define HOST_BROWSER 1

/* ... */
/* #undef HOST_WASI */

/* ... */
/* #undef HOST_X86 */

/* ... */
/* #undef HOST_AMD64 */

/* ... */
/* #undef HOST_ARM */

/* ... */
/* #undef HOST_ARM64 */

/* ... */
/* #undef HOST_POWERPC */

/* ... */
/* #undef HOST_POWERPC64 */

/* ... */
/* #undef HOST_S390X */

/* ... */
/* #undef HOST_MIPS */

/* ... */
/* #undef HOST_SPARC */

/* ... */
/* #undef HOST_SPARC64 */

/* Host is RISC-V */
/* #undef HOST_RISCV */

/* Host is 32-bit RISC-V */
/* #undef HOST_RISCV32 */

/* Host is 64-bit RISC-V */
/* #undef HOST_RISCV64 */

/* ... */
#define USE_GCC_ATOMIC_OPS 1

/* The JIT/AOT targets iOS */
/* #undef TARGET_IOS */

/* The JIT/AOT targets tvOS */
/* #undef TARGET_TVOS */

/* The JIT/AOT targets Mac Catalyst */
/* #undef TARGET_MACCAT */

/* The JIT/AOT targets OSX */
/* #undef TARGET_OSX */

/* The JIT/AOT targets Apple platforms */
/* #undef TARGET_MACH */

/* byte order of target */
#define TARGET_BYTE_ORDER G_LITTLE_ENDIAN

/* wordsize of target */
#define TARGET_SIZEOF_VOID_P 4

/* size of target machine integer registers */
#define SIZEOF_REGISTER 4

/* host or target doesn't allow unaligned memory access */
/* #undef NO_UNALIGNED_ACCESS */

/* Support for the visibility ("hidden") attribute */
/* #undef HAVE_VISIBILITY_HIDDEN */

/* Support for the deprecated attribute */
/* #undef HAVE_DEPRECATED */

/* Moving collector */
#define HAVE_MOVING_COLLECTOR 1

/* Defaults to concurrent GC */
#define HAVE_CONC_GC_AS_DEFAULT 1

/* Define to 1 if you have the `stpcpy' function. */
#define HAVE_STPCPY 1

/* Define to 1 if you have the `strtok_r' function. */
#define HAVE_STRTOK_R 1

/* Define to 1 if you have the `rewinddir' function. */
#define HAVE_REWINDDIR 1

/* Define to 1 if you have the `vasprintf' function. */
#define HAVE_VASPRINTF 1

/* Overridable allocator support enabled */
/* #undef ENABLE_OVERRIDABLE_ALLOCATORS */

/* Define to 1 if you have the `strndup' function. */
#define HAVE_STRNDUP 1

/* Define to 1 if you have the <getopt.h> header file. */
#define HAVE_GETOPT_H 1

/* Define to 1 if you have the <iconv.h> header file. */
#define HAVE_ICONV_H 1

/* Define to 1 if you have the `iconv' library (-liconv). */
/* #undef HAVE_LIBICONV */

/* Icall symbol map enabled */
/* #undef ENABLE_ICALL_SYMBOL_MAP */

/* Icall export enabled */
#define ENABLE_ICALL_EXPORT 1

/* Icall tables disabled */
#define DISABLE_ICALL_TABLES 1

/* QCalls disabled */
#define DISABLE_QCALLS 1

/* Have __thread keyword */
/* #undef MONO_KEYWORD_THREAD */

/* tls_model available */
/* #undef HAVE_TLS_MODEL_ATTR */

/* ARM v5 */
/* #undef HAVE_ARMV5 */

/* ARM v6 */
/* #undef HAVE_ARMV6 */

/* ARM v7 */
/* #undef HAVE_ARMV7 */

/* RISC-V FPABI is double-precision */
/* #undef RISCV_FPABI_DOUBLE */

/* RISC-V FPABI is single-precision */
/* #undef RISCV_FPABI_SINGLE */

/* RISC-V FPABI is soft float */
/* #undef RISCV_FPABI_SOFT */

/* Use malloc for each single mempool allocation */
/* #undef USE_MALLOC_FOR_MEMPOOLS */

/* Enable lazy gc thread creation by the embedding host. */
#define LAZY_GC_THREAD_CREATION 1

/* Enable cooperative stop-the-world garbage collection. */
/* #undef ENABLE_COOP_SUSPEND */

/* Enable hybrid suspend for GC stop-the-world */
/* #undef ENABLE_HYBRID_SUSPEND */

/* Enable feature experiments */
/* #undef ENABLE_EXPERIMENTS */

/* Enable experiment 'null' */
/* #undef ENABLE_EXPERIMENT_null */

/* Enable experiment 'Tiered Compilation' */
/* #undef ENABLE_EXPERIMENT_TIERED */

/* Enable checked build */
/* #undef ENABLE_CHECKED_BUILD */

/* Enable GC checked build */
/* #undef ENABLE_CHECKED_BUILD_GC */

/* Enable metadata checked build */
/* #undef ENABLE_CHECKED_BUILD_METADATA */

/* Enable thread checked build */
/* #undef ENABLE_CHECKED_BUILD_THREAD */

/* Enable private types checked build */
/* #undef ENABLE_CHECKED_BUILD_PRIVATE_TYPES */

/* Enable EventPipe library support */
#define ENABLE_PERFTRACING 1

/* Define to 1 if you have /usr/include/malloc.h. */
/* #undef HAVE_USR_INCLUDE_MALLOC_H */

/* The architecture this is running on */
#define MONO_ARCHITECTURE "wasm"

/* Disable banned functions from being used by the runtime */
#define MONO_INSIDE_RUNTIME 1

/* Version number of package */
#define VERSION "7.0.0.0"

/* Full version number of package */
#define FULL_VERSION "42.42.42.42424"

/* Define to 1 if you have the <dlfcn.h> header file. */
#define HAVE_DLFCN_H 1

/* Enable lazy gc thread creation by the embedding host */
#define LAZY_GC_THREAD_CREATION 1

/* Enable additional checks */
/* #undef ENABLE_CHECKED_BUILD */

/* Enable compile time checking that getter functions are used */
/* #undef ENABLE_CHECKED_BUILD_PRIVATE_TYPES */

/* Enable runtime GC Safe / Unsafe mode assertion checks (must set env var MONO_CHECK_MODE=gc) */
/* #undef ENABLE_CHECKED_BUILD_GC */

/* Enable runtime history of per-thread coop state transitions (must set env var MONO_CHECK_MODE=thread) */
/* #undef ENABLE_CHECKED_BUILD_THREAD */

/* Enable runtime checks of mempool references between metadata images (must set env var MONO_CHECK_MODE=metadata) */
/* #undef ENABLE_CHECKED_BUILD_METADATA */

/* Enable static linking of mono runtime components */
#define STATIC_COMPONENTS

/* Enable perf jit dump support */
/* #undef ENABLE_JIT_DUMP */

#if defined(ENABLE_LLVM) && defined(HOST_WIN32) && defined(TARGET_WIN32) && (!defined(TARGET_AMD64) || !defined(_MSC_VER))
#error LLVM for host=Windows and target=Windows is only supported on x64 MSVC build.
#endif

#endif
