#ifndef IL2CXX__PORTABLE__DEFINE_H
#define IL2CXX__PORTABLE__DEFINE_H

#ifdef __GNUC__
#define IL2CXX__PORTABLE__THREAD __thread
#define IL2CXX__PORTABLE__EXPORT
#define IL2CXX__PORTABLE__SUPPORTS_THREAD_EXPORT
#define IL2CXX__PORTABLE__SUPPORTS_COMPUTED_GOTO
#define IL2CXX__PORTABLE__ALWAYS_INLINE __attribute__((always_inline))
#define IL2CXX__PORTABLE__NOINLINE __attribute__((noinline))
#define IL2CXX__PORTABLE__FORCE_INLINE
#define IL2CXX__PORTABLE__DEFINE_EXPORT
#endif
#ifdef _MSC_VER
#define IL2CXX__PORTABLE__THREAD __declspec(thread)
#ifndef IL2CXX__PORTABLE__EXPORT
#define IL2CXX__PORTABLE__EXPORT __declspec(dllimport)
#endif
#define IL2CXX__PORTABLE__ALWAYS_INLINE
#define IL2CXX__PORTABLE__NOINLINE
#define IL2CXX__PORTABLE__FORCE_INLINE __forceinline
#define IL2CXX__PORTABLE__DEFINE_EXPORT __declspec(dllexport)
#endif

#ifdef _WIN32
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0400
#endif
#define NOMINMAX
#endif

#endif
