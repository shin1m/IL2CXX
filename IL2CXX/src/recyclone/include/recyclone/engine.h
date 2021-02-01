#ifndef RECYCLONE__ENGINE_H
#define RECYCLONE__ENGINE_H

#include "thread.h"
#include <deque>
#include <semaphore.h>

namespace recyclone
{

struct t_conductor
{
	bool v_running = true;
	bool v_quitting = false;
	std::mutex v_mutex;
	std::condition_variable v_wake;
	std::condition_variable v_done;

	void f_next(std::unique_lock<std::mutex>& a_lock)
	{
		v_running = false;
		v_done.notify_all();
		do v_wake.wait(a_lock); while (!v_running);
	}
	void f_exit()
	{
		std::lock_guard lock(v_mutex);
		v_running = false;
		v_done.notify_one();
	}
	void f_wake()
	{
		if (v_running) return;
		v_running = true;
		v_wake.notify_one();
	}
	void f_wait(std::unique_lock<std::mutex>& a_lock)
	{
		do v_done.wait(a_lock); while (v_running);
	}
	void f_quit()
	{
		std::unique_lock lock(v_mutex);
		v_running = v_quitting = true;
		v_wake.notify_one();
		f_wait(lock);
	}
};

template<typename T_type>
t_engine<T_type>* f_engine();

template<typename T_type>
class t_engine
{
	friend class t_object<T_type>;
	friend class t_weak_pointer<T_type>;
	friend struct t_thread<T_type>;
	friend t_engine* f_engine<T_type>();

protected:
	static inline thread_local t_engine* v_instance;

	t_conductor v_collector__conductor;
	size_t v_collector__threshold;
	size_t v_collector__tick = 0;
	size_t v_collector__wait = 0;
	size_t v_collector__epoch = 0;
	size_t v_collector__collect = 0;
	size_t v_collector__full = 0;
	t_heap<t_object<T_type>> v_object__heap;
	size_t v_object__lower = 0;
	bool v_object__reviving = false;
	std::mutex v_object__reviving__mutex;
	size_t v_object__release = 0;
	size_t v_object__collect = 0;
	sem_t v_epoch__received;
	sigset_t v_epoch__notsigusr2;
	struct sigaction v_epoch__old_sigusr1;
	struct sigaction v_epoch__old_sigusr2;
	t_thread<T_type>* v_thread__head = nullptr;
	t_thread<T_type>* v_thread__main;
	t_thread<T_type>* v_thread__finalizer = nullptr;
	std::mutex v_thread__mutex;
	std::condition_variable v_thread__condition;
	t_conductor v_finalizer__conductor;
	std::deque<t_object<T_type>*> v_finalizer__queue;
	bool v_finalizer__sleeping = false;
	uint8_t v_finalizer__awaken = 0;
	bool v_exiting = false;

	void f_object__return()
	{
		v_object__heap.f_return();
	}
	void f_free(t_object<T_type>* a_p)
	{
		a_p->v_count = 1;
		v_object__heap.f_free(a_p);
	}
	void f_free_as_release(t_object<T_type>* a_p)
	{
		++v_object__release;
		f_free(a_p);
	}
	void f_free_as_collect(t_object<T_type>* a_p)
	{
		++v_object__collect;
		f_free(a_p);
	}
	t_object<T_type>* f_object__find(void* a_p)
	{
		if (reinterpret_cast<uintptr_t>(a_p) & 127) return nullptr;
		auto p = v_object__heap.f_find(a_p);
		return p && p->v_type ? p : nullptr;
	}
	void f_epoch_suspend()
	{
		if (sem_post(&v_epoch__received) == -1) _exit(errno);
		sigsuspend(&v_epoch__notsigusr2);
		if (sem_post(&v_epoch__received) == -1) _exit(errno);
	}
	void f_epoch_send(pthread_t a_thread, int a_signal)
	{
		pthread_kill(a_thread, a_signal);
		while (sem_wait(&v_epoch__received) == -1) if (errno != EINTR) throw std::system_error(errno, std::generic_category());
	}
	void f_collector();
	void f_finalizer(void(*a_finalize)(t_object<T_type>*));
	size_t f_statistics();

public:
	struct t_options
	{
#ifdef NDEBUG
		size_t v_collector__threshold = 1024 * 64;
#else
		size_t v_collector__threshold = 64;
#endif
		bool v_verbose = false;
		bool v_verify = false;
	};

	const t_options& v_options;

	t_engine(const t_options& a_options);
	~t_engine();
	int f_exit(int a_code);
	void f_tick()
	{
		if (v_collector__conductor.v_running) return;
		std::lock_guard lock(v_collector__conductor.v_mutex);
		++v_collector__tick;
		v_collector__conductor.f_wake();
	}
	void f_wait()
	{
		std::unique_lock lock(v_collector__conductor.v_mutex);
		++v_collector__wait;
		v_collector__conductor.f_wake();
		v_collector__conductor.f_wait(lock);
	}
	RECYCLONE__ALWAYS_INLINE constexpr t_object<T_type>* f_object__allocate(size_t a_size)
	{
		auto p = v_object__heap.f_allocate(a_size);
		p->v_next = nullptr;
		return p;
	}
	void f_collect();
	void f_finalize();
	size_t f_load_count() const
	{
		size_t n = 0;
		v_object__heap.f_statistics([&](auto, auto, auto a_allocated, auto a_freed)
		{
			n += a_allocated - a_freed;
		});
		return n;
	}
	bool f_exiting() const
	{
		return v_exiting;
	}
};

template<typename T_type>
void t_engine<T_type>::f_collector()
{
	v_instance = this;
	if (v_options.v_verbose) std::fprintf(stderr, "collector starting...\n");
	t_object<T_type>::v_roots.v_next = t_object<T_type>::v_roots.v_previous = reinterpret_cast<t_object<T_type>*>(&t_object<T_type>::v_roots);
	while (true) {
		{
			std::unique_lock lock(v_collector__conductor.v_mutex);
			v_collector__conductor.f_next(lock);
			if (v_collector__conductor.v_quitting) break;
		}
		++v_collector__epoch;
		{
			std::lock_guard lock(v_object__reviving__mutex);
			v_object__reviving = false;
		}
		{
			std::lock_guard lock(v_thread__mutex);
			auto p = &v_thread__head;
			while (*p) {
				auto q = *p;
				auto active = q->v_done >= 0;
				if (active && q == v_thread__finalizer && q->v_done <= 0) {
					active = v_finalizer__awaken;
					if (active && v_finalizer__sleeping) --v_finalizer__awaken;
				}
				if (active) {
					auto tail = q->v_increments.v_tail;
					q->f_epoch();
					std::lock_guard lock(v_object__reviving__mutex);
					if (q->v_reviving) {
						size_t n = t_slot<T_type>::t_increments::V_SIZE;
						size_t epoch = (q->v_increments.v_tail + n - tail) % n;
						size_t reviving = (q->v_reviving + n - tail) % n;
						if (epoch < reviving)
							v_object__reviving = true;
						else
							q->v_reviving = nullptr;
					}
				}
				if (q->v_done < 3) {
					p = &q->v_next;
				} else {
					*p = q->v_next;
					delete q;
				}
			}
		}
		t_object<T_type>::f_collect();
		v_object__heap.f_flush();
	}
	if (v_options.v_verbose) std::fprintf(stderr, "collector quitting...\n");
	v_collector__conductor.f_exit();
}

template<typename T_type>
void t_engine<T_type>::f_finalizer(void(*a_finalize)(t_object<T_type>*))
{
	if (v_options.v_verbose) std::fprintf(stderr, "finalizer starting...\n");
	while (true) {
		{
			std::lock_guard lock(v_thread__mutex);
			v_finalizer__sleeping = true;
		}
		{
			std::unique_lock lock(v_finalizer__conductor.v_mutex);
			if (v_finalizer__conductor.v_quitting) break;
			v_finalizer__conductor.f_next(lock);
		}
		{
			std::lock_guard lock(v_thread__mutex);
			v_finalizer__sleeping = false;
			v_finalizer__awaken = 2;
		}
#ifndef NDEBUG
		[this, a_finalize]
		{
		char padding[4096];
		std::memset(padding, 0, sizeof(padding));
		[this, a_finalize]
		{
#endif
		while (true) {
			t_object<T_type>* p;
			{
				std::lock_guard lock(v_finalizer__conductor.v_mutex);
				if (v_finalizer__queue.empty()) break;
				p = v_finalizer__queue.front();
				v_finalizer__queue.pop_front();
			}
			p->v_finalizee = false;
			a_finalize(p);
			t_slot<T_type>::t_decrements::f_push(p);
		}
#ifndef NDEBUG
		}();
		}();
#endif
	}
	if (v_options.v_verbose) std::fprintf(stderr, "finalizer quitting...\n");
	v_finalizer__conductor.f_exit();
}

template<typename T_type>
size_t t_engine<T_type>::f_statistics()
{
	if (v_options.v_verbose) std::fprintf(stderr, "statistics:\n\tt_object:\n");
	size_t allocated = 0;
	size_t freed = 0;
	v_object__heap.f_statistics([&](auto a_rank, auto a_grown, auto a_allocated, auto a_freed)
	{
		if (v_options.v_verbose) std::fprintf(stderr, "\t\trank%zu: %zu: %zu - %zu = %zu\n", a_rank, a_grown, a_allocated, a_freed, a_allocated - a_freed);
		allocated += a_allocated;
		freed += a_freed;
	});
	if (v_options.v_verbose) {
		std::fprintf(stderr, "\t\ttotal: %zu - %zu = %zu, release = %zu, collect = %zu\n", allocated, freed, allocated - freed, v_object__release, v_object__collect);
		std::fprintf(stderr, "\tcollector: tick = %zu, wait = %zu, epoch = %zu, collect = %zu\n", v_collector__tick, v_collector__wait, v_collector__epoch, v_collector__collect);
	}
	return allocated - freed;
}

template<typename T_type>
t_engine<T_type>::t_engine(const t_options& a_options) : v_collector__threshold(a_options.v_collector__threshold), v_object__heap([]
{
	f_engine<T_type>()->f_wait();
}), v_options(a_options)
{
	v_instance = this;
	v_thread__main = new t_thread<T_type>();
	if (sem_init(&v_epoch__received, 0, 0) == -1) throw std::system_error(errno, std::generic_category());
	sigfillset(&v_epoch__notsigusr2);
	sigdelset(&v_epoch__notsigusr2, SIGUSR2);
	struct sigaction sa;
	sa.sa_handler = [](int)
	{
	};
	sigemptyset(&sa.sa_mask);
	sa.sa_flags = SA_RESTART;
	if (sigaction(SIGUSR2, &sa, &v_epoch__old_sigusr2) == -1) throw std::system_error(errno, std::generic_category());
	sa.sa_handler = [](int)
	{
		t_thread<T_type>::v_current->f_epoch_get();
		f_engine<T_type>()->f_epoch_suspend();
	};
	sigaddset(&sa.sa_mask, SIGUSR2);
	if (sigaction(SIGUSR1, &sa, &v_epoch__old_sigusr1) == -1) throw std::system_error(errno, std::generic_category());
	v_thread__main->f_initialize(this);
	std::unique_lock lock(v_collector__conductor.v_mutex);
	std::thread(&t_engine::f_collector, this).detach();
	v_collector__conductor.f_wait(lock);
}

template<typename T_type>
t_engine<T_type>::~t_engine()
{
	{
		v_thread__main->f_epoch_get();
		std::lock_guard lock(v_thread__mutex);
		++v_thread__main->v_done;
	}
	f_wait();
	f_wait();
	f_wait();
	f_wait();
	v_collector__conductor.f_quit();
	assert(!v_thread__head);
	if (sem_destroy(&v_epoch__received) == -1) std::exit(errno);
	if (sigaction(SIGUSR1, &v_epoch__old_sigusr1, NULL) == -1) std::exit(errno);
	if (sigaction(SIGUSR2, &v_epoch__old_sigusr2, NULL) == -1) std::exit(errno);
	if (f_statistics() <= 0) return;
	if (v_options.v_verbose) {
		std::map<T_type*, size_t> leaks;
		for (auto& x : v_object__heap.f_blocks())
			if (x.first->v_rank < 7) {
				auto p0 = reinterpret_cast<char*>(x.first);
				auto p1 = p0 + x.second;
				auto unit = 128 << x.first->v_rank;
				for (; p0 < p1; p0 += unit) {
					auto p = reinterpret_cast<t_object<T_type>*>(p0);
					if (p->v_type) ++leaks[p->v_type];
				}
			} else {
				++leaks[x.first->v_type];
			}
		for (const auto& x : leaks) std::fprintf(stderr, "%p: %zu\n", x.first, x.second);
	}
	std::terminate();
}

template<typename T_type>
int t_engine<T_type>::f_exit(int a_code)
{
	{
		std::unique_lock lock(v_thread__mutex);
		auto tail = v_thread__finalizer ? v_thread__finalizer : v_thread__main;
		while (true) {
			auto p = v_thread__head;
			while (p != tail && (p->v_done > 0 || p->v_background)) p = p->v_next;
			if (p == tail) break;
			v_thread__condition.wait(lock);
		}
		v_exiting = true;
	}
	if (v_options.v_verify) {
		f_object__return();
		{
			std::lock_guard lock(v_collector__conductor.v_mutex);
			if (v_collector__full++ <= 0) v_collector__threshold = 0;
		}
		if (!v_thread__finalizer) return a_code;
		f_wait();
		f_wait();
		f_wait();
		f_wait();
		assert(v_thread__head == v_thread__finalizer);
		v_finalizer__conductor.f_quit();
		std::unique_lock lock(v_thread__mutex);
		while (v_thread__head->v_next && v_thread__head->v_done <= 0) v_thread__condition.wait(lock);
		return a_code;
	} else {
		if (v_options.v_verbose) f_statistics();
		std::exit(a_code);
	}
}

template<typename T_type>
void t_engine<T_type>::f_collect()
{
	{
		std::lock_guard lock(v_collector__conductor.v_mutex);
		if (v_collector__full++ <= 0) v_collector__threshold = 0;
	}
	f_wait();
	f_wait();
	f_wait();
	f_wait();
	{
		std::lock_guard lock(v_collector__conductor.v_mutex);
		if (--v_collector__full <= 0) v_collector__threshold = v_options.v_collector__threshold;
	}
}

template<typename T_type>
void t_engine<T_type>::f_finalize()
{
	std::unique_lock lock(v_finalizer__conductor.v_mutex);
	v_finalizer__conductor.f_wake();
	v_finalizer__conductor.f_wait(lock);
}

template<typename T_type>
inline t_engine<T_type>* f_engine()
{
	return t_engine<T_type>::v_instance;
}

}

#endif
