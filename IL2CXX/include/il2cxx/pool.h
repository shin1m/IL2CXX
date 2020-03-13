#ifndef IL2CXX__POOL_H
#define IL2CXX__POOL_H

#include "define.h"
#include <list>
#include <map>
#include <mutex>
#include <new>
#include <cstddef>

namespace il2cxx
{

template<typename T, typename T_wait>
class t_pool
{
	struct t_chunk
	{
		T* v_head;
		size_t v_size;
	};
	template<size_t A_rank, size_t A_size>
	struct t_of
	{
		std::list<t_chunk> v_chunks;
		std::mutex v_mutex;
		size_t v_allocated = 0;
		size_t v_returned = 0;
		size_t v_freed = 0;

		void f_grow(t_pool& a_pool)
		{
			auto size = 128 << A_rank;
			auto block = new char[size * A_size];
			auto p = block;
			for (size_t i = 1; i < A_size; ++i) {
				auto q = new(p) T;
				q->v_next = reinterpret_cast<T*>(p += size);
				q->v_rank = A_rank;
			}
			auto q = new(p) T;
			q->v_next = nullptr;
			q->v_rank = A_rank;
			q = reinterpret_cast<T*>(block);
			v_chunks.push_back({q, A_size});
			std::lock_guard<std::mutex> lock(a_pool.v_mutex);
			a_pool.v_blocks.emplace(q, A_size);
		}
		T* f_allocate(t_pool* a_pool)
		{
			std::lock_guard<std::mutex> lock(v_mutex);
			if (v_chunks.empty()) {
				if (!a_pool) return nullptr;
				f_grow(*a_pool);
			}
			auto p = v_chunks.front().v_head;
			v_allocated += v_chunks.front().v_size;
			v_chunks.pop_front();
			return p;
		}
		void f_return(size_t a_n)
		{
			std::lock_guard<std::mutex> lock(v_mutex);
			v_chunks.push_back({v_head<A_rank>, a_n});
			v_head<A_rank> = nullptr;
			v_returned += a_n;
		}
		void f_return()
		{
			size_t n = 0;
			for (auto p = v_head<A_rank>; p; p = p->v_next) ++n;
			if (n > 0) f_return(n);
		}
		void f_flush()
		{
			f_return(v_freed);
			v_freed = 0;
		}
		void f_free(T* a_p)
		{
			a_p->v_next = v_head<A_rank>;
			v_head<A_rank> = a_p;
			if (++v_freed >= A_size) f_flush();
		}
		size_t f_live() const
		{
			return v_allocated - v_returned - v_freed;
		}
	};

	template<size_t A_rank>
	static IL2CXX__PORTABLE__THREAD T* v_head;

	T_wait v_wait;
	std::map<T*, size_t> v_blocks;
	std::mutex v_mutex;
	t_of<0, 4096> v_of0;
	t_of<1, 4096> v_of1;
	t_of<2, 4096> v_of2;
	t_of<3, 4096> v_of3;
	size_t v_allocated = 0;
	size_t v_freed = 0;

	template<size_t A_rank, size_t A_size>
	T* f_allocate(t_of<A_rank, A_size>& a_of)
	{
		auto p = v_head<A_rank>;
		if (!p) {
			p = a_of.f_allocate(nullptr);
			if (!p) {
				v_wait();
				p = a_of.f_allocate(this);
			}
		}
		v_head<A_rank> = p->v_next;
		return p;
	}

public:
	t_pool(T_wait&& a_wait) : v_wait(std::move(a_wait))
	{
	}
	~t_pool()
	{
		for (auto& x : v_blocks) delete x.first;
	}
	size_t f_live() const
	{
		return v_of0.f_live() + v_of1.f_live() + v_of2.f_live() + v_of3.f_live() + v_allocated - v_freed;
	}
	template<typename T_each>
	void f_statistics(T_each a_each) const
	{
		a_each(0, v_of0.v_allocated, v_of0.v_returned);
		a_each(1, v_of1.v_allocated, v_of1.v_returned);
		a_each(2, v_of2.v_allocated, v_of2.v_returned);
		a_each(3, v_of3.v_allocated, v_of3.v_returned);
		a_each(4, v_allocated, v_freed);
	}
	void f_grow()
	{
		v_of0.f_grow(*this);
		v_of1.f_grow(*this);
		v_of2.f_grow(*this);
		v_of3.f_grow(*this);
	}
	T* f_allocate(size_t a_size)
	{
		size_t n = a_size >> 7;
		if (n == 0) return f_allocate(v_of0);
		n >>= 1;
		if (n == 0) return f_allocate(v_of1);
		n >>= 1;
		if (n == 0) return f_allocate(v_of2);
		n >>= 1;
		if (n == 0) return f_allocate(v_of3);
		std::lock_guard<std::mutex> lock(v_mutex);
		++v_allocated;
		auto p = new(new char[a_size]) T;
		p->v_rank = 4;
		v_blocks.emplace(p, 1);
		return p;
	}
	void f_return()
	{
		v_of0.f_return();
		v_of1.f_return();
		v_of2.f_return();
		v_of3.f_return();
	}
	void f_flush()
	{
		if (v_of0.v_freed > 0) v_of0.f_flush();
		if (v_of1.v_freed > 0) v_of1.f_flush();
		if (v_of2.v_freed > 0) v_of2.f_flush();
		if (v_of3.v_freed > 0) v_of3.f_flush();
	}
	void f_free(T* a_p)
	{
		switch (a_p->v_rank) {
		case 0:
			v_of0.f_free(a_p);
			break;
		case 1:
			v_of1.f_free(a_p);
			break;
		case 2:
			v_of2.f_free(a_p);
			break;
		case 3:
			v_of3.f_free(a_p);
			break;
		default:
			std::lock_guard<std::mutex> lock(v_mutex);
			v_blocks.erase(a_p);
			delete a_p;
			++v_freed;
		}
	}
	T* f_find(void* a_p)
	{
		std::lock_guard<std::mutex> lock(v_mutex);
		auto i = v_blocks.lower_bound(static_cast<T*>(a_p));
		if (i == v_blocks.end() || i->first != a_p) {
			if (i == v_blocks.begin()) return nullptr;
			--i;
		}
		if (i->second == 1) return a_p == i->first ? static_cast<T*>(a_p) : nullptr;
		auto j = static_cast<char*>(a_p) - reinterpret_cast<char*>(i->first);
		auto rank = i->first->v_rank + 7;
		return j >> rank < i->second && (j & (1 << rank) - 1) == 0 ? static_cast<T*>(a_p) : nullptr;
	}
};

template<typename T, typename T_wait>
template<size_t A_rank>
IL2CXX__PORTABLE__THREAD T* t_pool<T, T_wait>::v_head;

}

#endif
