#ifndef IL2CXX__POOL_H
#define IL2CXX__POOL_H

#include <cstddef>
#include <list>
#include <mutex>
#include <new>
#include "define.h"

namespace il2cxx
{

template<typename T, size_t A_size>
class t_shared_pool
{
	struct t_block
	{
		t_block* v_next;
		T v_cells[A_size];
	};
	struct t_chunk
	{
		decltype(T::v_next) v_head;
		size_t v_size;
	};

	t_block* v_blocks = nullptr;
	std::list<t_chunk> v_chunks;
	std::mutex v_mutex;
	size_t v_allocated = 0;
	size_t v_freed = 0;

public:
	void f_clear()
	{
		while (v_blocks) {
			auto block = v_blocks;
			v_blocks = block->v_next;
			delete block;
		}
	}
	void f_grow();
	size_t f_allocated() const
	{
		return v_allocated;
	}
	size_t f_freed() const
	{
		return v_freed;
	}
	decltype(T::v_next) f_allocate(bool a_grow = true)
	{
		std::lock_guard<std::mutex> lock(v_mutex);
		if (v_chunks.empty()) {
			if (!a_grow) return nullptr;
			f_grow();
			if (v_chunks.empty()) return nullptr;
		}
		auto p = v_chunks.front().v_head;
		v_allocated += v_chunks.front().v_size;
		v_chunks.pop_front();
		return p;
	}
	void f_free(decltype(T::v_next) a_p, size_t a_n)
	{
		std::lock_guard<std::mutex> lock(v_mutex);
		v_chunks.push_back({a_p, a_n});
		v_freed += a_n;
	}
};

template<typename T, size_t A_size>
void t_shared_pool<T, A_size>::f_grow()
{
	auto block = new t_block();
	block->v_next = v_blocks;
	v_blocks = block;
	T* p = block->v_cells;
	for (size_t i = 1; i < A_size; ++i) {
		p->v_next = p + 1;
		++p;
	}
	p->v_next = nullptr;
	v_chunks.push_back({block->v_cells, A_size});
}

template<typename T>
class t_local_pool
{
	static IL2CXX__PORTABLE__THREAD decltype(T::v_next) v_head;

public:
	template<typename T_allocate>
	static decltype(T::v_next) f_allocate(T_allocate a_allocate)
	{
		auto p = v_head;
		if (!p) p = a_allocate();
		v_head = p->v_next;
		return p;
	}
	static void f_free(decltype(T::v_next) a_p)
	{
		a_p->v_next = v_head;
		v_head = a_p;
	}
	static decltype(T::v_next) f_detach()
	{
		auto p = v_head;
		v_head = nullptr;
		return p;
	}
};

template<typename T>
IL2CXX__PORTABLE__THREAD decltype(T::v_next) t_local_pool<T>::v_head;

}

#endif
