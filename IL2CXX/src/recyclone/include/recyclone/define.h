#ifndef RECYCLONE__DEFINE_H
#define RECYCLONE__DEFINE_H

#ifdef __GNUC__
#define RECYCLONE__THREAD __thread
#define RECYCLONE__ALWAYS_INLINE __attribute__((always_inline))
#define RECYCLONE__NOINLINE __attribute__((noinline))
#define RECYCLONE__FORCE_INLINE
#endif
#ifdef _MSC_VER
#define RECYCLONE__THREAD __declspec(thread)
#define RECYCLONE__ALWAYS_INLINE
#define RECYCLONE__NOINLINE
#define RECYCLONE__FORCE_INLINE __forceinline
#endif

#endif
