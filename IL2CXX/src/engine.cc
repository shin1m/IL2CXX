#include <il2cxx/engine.h>

namespace il2cxx
{

void t__type::f_scan(t_object* a_this, t_scan a_scan)
{
}

t_scoped<t_slot> t__type::f_clone(const t_object* a_this)
{
	throw std::logic_error("not supported.");
}

void t__type::f_copy(const char* a_from, size_t a_n, char* a_to)
{
	std::copy_n(reinterpret_cast<const t_slot*>(a_from), a_n, reinterpret_cast<t_slot*>(a_to));
}

IL2CXX__PORTABLE__THREAD size_t t_engine::v_local_object__allocated;

void t_engine::f_pools__return()
{
	v_object__pool0.f_return_all();
	v_object__pool1.f_return_all();
	v_object__pool2.f_return_all();
	v_object__pool3.f_return_all();
	v_object__allocated += v_local_object__allocated;
	v_local_object__allocated = 0;
}

void t_engine::f_collector()
{
	t_slot::v_collector = this;
	if (v_options.v_verbose) std::fprintf(stderr, "collector starting...\n");
	t_object::v_roots.v_next = t_object::v_roots.v_previous = reinterpret_cast<t_object*>(&t_object::v_roots);
	while (true) {
		{
			std::unique_lock<std::mutex> lock(v_collector__mutex);
			v_collector__running = false;
			v_collector__done.notify_all();
			do v_collector__wake.wait(lock); while (!v_collector__running);
		}
		if (v_collector__quitting) {
			if (v_options.v_verbose) std::fprintf(stderr, "collector quitting...\n");
			std::lock_guard<std::mutex> lock(v_collector__mutex);
			v_collector__running = false;
			v_collector__done.notify_one();
			break;
		}
		++v_collector__epoch;
		{
			std::lock_guard<std::mutex> lock(v_object__reviving__mutex);
			v_object__reviving = false;
		}
		{
			std::lock_guard<std::mutex> lock(v_thread__mutex);
			if (v_thread__internals) v_thread__internals->f_epoch_request();
			for (auto p = &v_thread__internals; *p;) {
				auto q = *p;
				q->f_epoch_wait();
				if (q->v_done > 0) ++q->v_done;
				if (q->v_done < 3)
					p = &q->v_next;
				else
					*p = q->v_next;
				if (*p) (*p)->f_epoch_request();
				auto tail = q->v_increments.v_tail + 1;
				q->v_increments.f_flush();
				q->v_decrements.f_flush();
				{
					std::lock_guard<std::mutex> lock(v_object__reviving__mutex);
					if (q->v_reviving) {
						size_t n = t_slot::t_increments::V_SIZE;
						size_t epoch = (q->v_increments.v_tail + 1 + n - tail) % n;
						size_t reviving = (q->v_reviving + n - tail) % n;
						if (epoch > reviving)
							q->v_reviving = nullptr;
						else
							v_object__reviving = true;
					}
				}
				if (q->v_done >= 3) delete q;
			}
		}
		t_object::f_collect();
		if (v_object__pool0.v_freed > 0) v_object__pool0.f_return();
		if (v_object__pool1.v_freed > 0) v_object__pool1.f_return();
		if (v_object__pool2.v_freed > 0) v_object__pool2.f_return();
		if (v_object__pool3.v_freed > 0) v_object__pool3.f_return();
	}
}

t_engine::t_engine(const t_options& a_options, size_t a_count, char** a_arguments) : v_options(a_options)
{
	v_thread__internals->f_initialize();
	v_object__pool0.f_grow();
	v_object__pool1.f_grow();
	v_object__pool2.f_grow();
	v_object__pool3.f_grow();
	v_thread = f__new_zerod<t_System_2eThreading_2eThread>();
	v_thread->v__internal = v_thread__internals;
	t_System_2eThreading_2eThread::v__current = v_thread;
	{
		std::unique_lock<std::mutex> lock(v_collector__mutex);
		std::thread(&t_engine::f_collector, this).detach();
		while (v_collector__running) v_collector__done.wait(lock);
	}
}

t_engine::~t_engine()
{
	{
		t_epoch_region region;
		std::unique_lock<std::mutex> lock(v_thread__mutex);
		auto internal = v_thread->v__internal;
		while (true) {
			auto p = v_thread__internals;
			while (p == internal || p && p->v_done > 0) p = p->v_next;
			if (!p) break;
			v_thread__condition.wait(lock);
		}
		++v_thread->v__internal->v_done;
	}
	v_thread.f__destruct();
	f_pools__return();
	v_options.v_collector__threshold = 0;
	f_wait();
	f_wait();
	f_wait();
	f_wait();
	{
		std::unique_lock<std::mutex> lock(v_collector__mutex);
		v_collector__running = v_collector__quitting = true;
		v_collector__wake.notify_one();
		do v_collector__done.wait(lock); while (v_collector__running);
	}
	assert(!v_thread__internals);
	v_object__pool0.f_clear();
	v_object__pool1.f_clear();
	v_object__pool2.f_clear();
	v_object__pool3.f_clear();
	if (v_options.v_verbose) {
		std::fprintf(stderr, "statistics:\n\tt_object:\n");
		size_t allocated = 0;
		size_t freed = 0;
		auto f = [&](auto& a_pool, size_t a_rank)
		{
			size_t x = a_pool.f_allocated();
			size_t y = a_pool.f_freed();
			std::fprintf(stderr, "\t\trank%zu: %zu - %zu = %zu\n", a_rank, x, y, x - y);
			allocated += x;
			freed += y;
		};
		f(v_object__pool0, 0);
		f(v_object__pool1, 1);
		f(v_object__pool2, 2);
		f(v_object__pool3, 3);
		std::fprintf(stderr, "\t\trank4: %zu - %zu = %zu\n", static_cast<size_t>(v_object__allocated), v_object__freed, v_object__allocated - v_object__freed);
		allocated += v_object__allocated;
		freed += v_object__freed;
		std::fprintf(stderr, "\t\ttotal: %zu - %zu = %zu, release = %zu, collect = %zu\n", allocated, freed, allocated - freed, v_object__release, v_object__collect);
		std::fprintf(stderr, "\tcollector: tick = %zu, wait = %zu, epoch = %zu, collect = %zu\n", v_collector__tick, v_collector__wait, v_collector__epoch, v_collector__collect);
		if (allocated != freed) std::exit(EXIT_FAILURE);
	}
}

}
