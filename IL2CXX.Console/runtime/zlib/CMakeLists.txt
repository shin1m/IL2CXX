cmake_minimum_required(VERSION 3.16)
project(zlib)
add_library(zlib STATIC
	adler32.c
	compress.c
	crc32.c
	uncompr.c
	deflate.c
	gzguts.h
	trees.c
	zutil.c
	inflate.c
	infback.c
	inftrees.c
	inffast.c
	crc32.h
	deflate.h
	inffast.h
	inffixed.h
	inflate.h
	inftrees.h
	trees.h
	zconf.h
	zlib.h
	zutil.h
	)
target_compile_options(zlib PRIVATE "-pthread")
