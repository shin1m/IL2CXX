CLEANFILES = *.gcda *.gcno *.gcov

AM_CPPFLAGS = -I$(top_srcdir)/include
AM_CXXFLAGS = -std=c++17
AM_LDFLAGS = -lpthread
if DEBUG
AM_CXXFLAGS += -g -fprofile-arcs -ftest-coverage
else
AM_CPPFLAGS += -DNDEBUG
AM_CXXFLAGS += -O3
endif
if PROFILE
AM_CXXFLAGS += -pg
AM_LDFLAGS += -pg
endif
AM_DEFAULT_SOURCE_EXT = .cc
check_PROGRAMS = \
	test_empty \
	test_collector \
	test_cyclic \
	test_large \
	test_threads \
	test_finalizer \
	test_background \
	test_monitor \
	test_weak_pointer \
	test_type_object
TESTS = $(check_PROGRAMS)
