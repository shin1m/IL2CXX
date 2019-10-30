#include <il2cxx/engine.h>

namespace il2cxx
{

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
			std::unique_lock<std::mutex> lock(v_collector__conductor.v_mutex);
			v_collector__conductor.f__next(lock);
			if (v_collector__conductor.v_quitting) break;
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
	if (v_options.v_verbose) std::fprintf(stderr, "collector quitting...\n");
	v_collector__conductor.f_exit();
}

void t_engine::f_finalizer()
{
	if (v_options.v_verbose) std::fprintf(stderr, "finalizer starting...\n");
	while (true) {
		{
			t_epoch_region region;
			std::unique_lock<std::mutex> lock(v_finalizer__conductor.v_mutex);
			if (v_finalizer__conductor.v_quitting) break;
			v_finalizer__conductor.f__next(lock);
		}
		while (true) {
			t_object* p;
			{
				t_epoch_region region;
				std::unique_lock<std::mutex> lock(v_finalizer__conductor.v_mutex);
				if (v_finalizer__queue.empty()) break;
				p = v_finalizer__queue.front();
				v_finalizer__queue.pop_front();
			}
			p->f_type()->f_suppress_finalize(p);
			f_finalize(p);
		}
	}
	if (v_options.v_verbose) std::fprintf(stderr, "finalizer quitting...\n");
	t_epoch_region region;
	v_finalizer__conductor.f_exit();
}

t_engine::t_engine(const t_options& a_options, size_t a_count, char** a_arguments) : v_options(a_options), v_collector__threshold(v_options.v_collector__threshold)
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
		std::unique_lock<std::mutex> lock(v_collector__conductor.v_mutex);
		std::thread(&t_engine::f_collector, this).detach();
		v_collector__conductor.f__wait(lock);
	}
	{
		auto finalizer = f__new_zerod<t_System_2eThreading_2eThread>();
		t_epoch_region region;
		std::unique_lock<std::mutex> lock(v_finalizer__conductor.v_mutex);
		finalizer->f__start([this]
		{
			f_finalizer();
		});
		v_finalizer__conductor.f__wait(lock);
	}
}

t_engine::~t_engine()
{
	f_wait();
	f_wait();
	f_wait();
	f_wait();
	v_collector__conductor.f_quit();
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

void t_engine::f_shutdown()
{
	{
		t_epoch_region region;
		std::unique_lock<std::mutex> lock(v_thread__mutex);
		auto internal = v_thread->v__internal;
		while (true) {
			auto p = v_thread__internals;
			while (p && (p->v_done > 0 || p == internal || p->v_next == internal)) p = p->v_next;
			if (!p) break;
			v_thread__condition.wait(lock);
		}
	}
	v_shuttingdown = true;
	f_pools__return();
	{
		std::unique_lock<std::mutex> lock(v_collector__conductor.v_mutex);
		if (v_collector__full++ <= 0) v_collector__threshold = 0;
	}
	f_wait();
	f_wait();
	f_wait();
	f_wait();
	assert(!v_thread__internals->v_next->v_next);
	{
		t_epoch_region region;
		v_finalizer__conductor.f_quit();
		std::unique_lock<std::mutex> lock(v_thread__mutex);
		while (v_thread__internals->v_next && v_thread__internals->v_done <= 0) v_thread__condition.wait(lock);
	}
	{
		auto internal = v_thread->v__internal;
		v_thread.f__destruct();
		t_epoch_region region;
		std::lock_guard<std::mutex> lock(v_thread__mutex);
		++internal->v_done;
	}
}

void t_engine::f_collect()
{
	{
		std::unique_lock<std::mutex> lock(v_collector__conductor.v_mutex);
		if (v_collector__full++ <= 0) v_collector__threshold = 0;
	}
	f_wait();
	f_wait();
	f_wait();
	f_wait();
	{
		std::unique_lock<std::mutex> lock(v_collector__conductor.v_mutex);
		if (--v_collector__full <= 0) v_collector__threshold = v_options.v_collector__threshold;
	}
}

void t_engine::f_finalize()
{
	t_epoch_region region;
	std::unique_lock<std::mutex> lock(v_finalizer__conductor.v_mutex);
	v_finalizer__conductor.f__wake();
	v_finalizer__conductor.f__wait(lock);
}

std::u16string f__u16string(std::string_view a_x)
{
	std::vector<char16_t> cs;
	std::mbstate_t state{};
	char16_t c;
	auto p = a_x.data();
	auto q = p + a_x.size();
	while (p < q) {
		auto n = std::mbrtoc16(&c, p, q - p, &state);
		switch (n) {
		case size_t(-3):
			cs.push_back(c);
			break;
		case size_t(-2):
			p = q;
			break;
		case size_t(-1):
			++p;
			break;
		case 0:
			cs.push_back('\0');
			++p;
			break;
		default:
			cs.push_back(c);
			p += n;
			break;
		}
	}
	if (std::mbrtoc16(&c, p, 0, &state) == size_t(-3)) cs.push_back(c);
	return {cs.begin(), cs.end()};
}

std::string f__string(std::u16string_view a_x)
{
	std::vector<char> cs;
	std::mbstate_t state{};
	char mb[MB_LEN_MAX];
	for (auto c : a_x) {
		auto n = std::c16rtomb(mb, c, &state);
		if (n != size_t(-1)) cs.insert(cs.end(), mb, mb + n);
	}
	auto n = std::c16rtomb(mb, u'\0', &state);
	if (n != size_t(-1) && n > 1) cs.insert(cs.end(), mb, mb + n - 1);
	return {cs.begin(), cs.end()};
}

}
