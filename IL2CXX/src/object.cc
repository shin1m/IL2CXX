#include <il2cxx/object.h>

namespace il2cxx
{

IL2CXX__PORTABLE__THREAD decltype(t_object::v_roots) t_object::v_roots;
IL2CXX__PORTABLE__THREAD t_object* t_object::v_scan_stack;
IL2CXX__PORTABLE__THREAD t_object* t_object::v_cycle;
IL2CXX__PORTABLE__THREAD t_object* t_object::v_cycles;

void t_object::f_collect()
{
	while (v_cycles) {
		std::lock_guard<std::mutex> lock(f_engine()->v_object__reviving__mutex);
		auto cycle = v_cycles;
		v_cycles = cycle->v_next_cycle;
		auto p = cycle;
		auto mutated = [&]
		{
			if (f_engine()->v_object__reviving)
				do if (p->v_color != e_color__ORANGE || p->v_cyclic > 0 || p->v_type->v_revive) return true; while ((p = p->v_next) != cycle);
			else
				do if (p->v_color != e_color__ORANGE || p->v_cyclic > 0) return true; while ((p = p->v_next) != cycle);
			return false;
		};
		if (mutated()) {
			p = cycle;
			auto q = p->v_next;
			if (p->v_color == e_color__ORANGE) {
				p->v_color = e_color__PURPLE;
				f_append(p);
			} else if (p->v_color == e_color__PURPLE) {
				f_append(p);
			} else {
				p->v_color = e_color__BLACK;
				p->v_next = nullptr;
			}
			while (q != cycle) {
				p = q;
				q = p->v_next;
				if (p->v_color == e_color__PURPLE) {
					f_append(p);
				} else {
					p->v_color = e_color__BLACK;
					p->v_next = nullptr;
				}
			}
		} else {
			// TODO: enqueue to-be-finalized objects to the finalizer thread.
			do p->v_color = e_color__RED; while ((p = p->v_next) != cycle);
			do p->f_cyclic_decrement(); while ((p = p->v_next) != cycle);
			do {
				auto q = p->v_next;
				f_engine()->f_free_as_collect(p);
				p = q;
			} while (p != cycle);
		}
	}
	auto roots = reinterpret_cast<t_object*>(&v_roots);
	if (roots->v_next == roots) return;
	{
		size_t live = f_engine()->v_object__pool0.f_live() + f_engine()->v_object__pool1.f_live() + f_engine()->v_object__pool2.f_live() + f_engine()->v_object__pool3.f_live() + f_engine()->v_object__allocated - f_engine()->v_object__freed;
		auto& lower = f_engine()->v_object__lower;
		if (live < lower) lower = live;
		if (live - lower < f_engine()->v_options.v_collector__threshold) return;
		lower = live;
		++f_engine()->v_collector__collect;
		auto p = roots;
		auto q = p->v_next;
		do {
			assert(q->v_count > 0);
			if (q->v_color == e_color__PURPLE) {
				q->f_mark_gray();
				p = q;
			} else {
				p->v_next = q->v_next;
				q->v_next = nullptr;
			}
			q = p->v_next;
		} while (q != roots);
	}
	if (roots->v_next == roots) {
		roots->v_previous = roots;
		return;
	}
	{
		auto p = roots->v_next;
		do {
			p->f_scan_gray();
			p = p->v_next;
		} while (p != roots);
	}
	do {
		auto p = roots->v_next;
		roots->v_next = p->v_next;
		if (p->v_color == e_color__WHITE) {
			p->f_collect_white();
			v_cycle->v_next_cycle = v_cycles;
			v_cycles = v_cycle;
		} else {
			p->v_next = nullptr;
		}
	} while (roots->v_next != roots);
	roots->v_previous = roots;
	for (auto cycle = v_cycles; cycle; cycle = cycle->v_next_cycle) {
		auto p = cycle;
		do {
			p->v_color = e_color__RED;
			p->v_cyclic = p->v_count;
		} while ((p = p->v_next) != cycle);
		do p->f_step<&t_object::f_scan_red>(); while ((p = p->v_next) != cycle);
		do p->v_color = e_color__ORANGE; while ((p = p->v_next) != cycle);
	}
}

void t_object::f_cyclic_decrement()
{
	v_type->f_scan(this, f_push_and_clear<&t_object::f_cyclic_decrement_push>);
	v_type->f_cyclic_decrement_push();
}

}
