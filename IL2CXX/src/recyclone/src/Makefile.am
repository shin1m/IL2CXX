noinst_LIBRARIES = librecyclon.a

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
librecyclon_a_SOURCES = object.cc thread.cc engine.cc
