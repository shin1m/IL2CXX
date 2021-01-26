#ifndef RECYCLONE__DEFINE_H
#define RECYCLONE__DEFINE_H

#ifdef __GNUC__
#define RECYCLONE__THREAD __thread
#define RECYCLONE__EXPORT
#define RECYCLONE__ALWAYS_INLINE __attribute__((always_inline))
#define RECYCLONE__NOINLINE __attribute__((noinline))
#define RECYCLONE__FORCE_INLINE
#define RECYCLONE__DEFINE_EXPORT
#endif
#ifdef _MSC_VER
#define RECYCLONE__THREAD __declspec(thread)
#ifndef RECYCLONE__EXPORT
#define RECYCLONE__EXPORT __declspec(dllimport)
#endif
#define RECYCLONE__ALWAYS_INLINE
#define RECYCLONE__NOINLINE
#define RECYCLONE__FORCE_INLINE __forceinline
#define RECYCLONE__DEFINE_EXPORT __declspec(dllexport)
#endif

#ifdef _WIN32
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0400
#endif
#define NOMINMAX
#endif

#endif
