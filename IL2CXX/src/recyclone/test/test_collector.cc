#include "pair.h"

template<typename T>
struct t_fix : private T
{
	template<typename U>
	explicit constexpr t_fix(U&& x) noexcept : T(std::forward<U>(x))
	{
	}
	template<typename... As>
	constexpr decltype(auto) operator()(As&&... as) const
	{
		return T::operator()(*this, std::forward<As>(as)...);
	}
};

template<typename T>
t_fix(T&&) -> t_fix<std::decay_t<T>>;

template<typename T_move>
t_pair* f_hanoi(t_pair* a_tower, T_move a_move)
{
	return t_fix([&](auto step, t_pair* a_height, t_pair* a_towers, size_t a_from, size_t a_via, size_t a_to) -> t_pair*
	{
		return a_height
			? step(static_cast<t_pair*>(a_height->v_tail), a_move(
				step(static_cast<t_pair*>(a_height->v_tail), a_towers, a_from, a_to, a_via),
				a_from, a_to
			), a_via, a_from, a_to)
			: a_move(a_towers, a_from, a_to);
	})(static_cast<t_pair*>(a_tower->v_tail), f_new<t_pair>(a_tower, f_new<t_pair>(nullptr, f_new<t_pair>(nullptr))), 0, 1, 2);
}

t_object<t_type>* f_get(t_pair* a_xs, size_t a_i)
{
	return a_i > 0 ? f_get(static_cast<t_pair*>(a_xs->v_tail), a_i - 1) : static_cast<t_object<t_type>*>(a_xs->v_head);
}

t_pair* f_put(t_pair* a_xs, size_t a_i, t_object<t_type>* a_x)
{
	return a_i > 0
		? f_new<t_pair>(a_xs->v_head, f_put(static_cast<t_pair*>(a_xs->v_tail), a_i - 1, a_x))
		: f_new<t_pair>(a_x, a_xs->v_tail);
}

int main(int argc, char* argv[])
{
	t_engine<t_type>::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine<t_type> engine(options);
	auto towers = f_hanoi(
		f_new<t_pair>(f_new<t_symbol>("a"sv),
		f_new<t_pair>(f_new<t_symbol>("b"sv),
		f_new<t_pair>(f_new<t_symbol>("c"sv),
		f_new<t_pair>(f_new<t_symbol>("d"sv),
		f_new<t_pair>(f_new<t_symbol>("e"sv)
	))))), [](auto a_towers, auto a_from, auto a_to)
	{
		std::printf("%s\n", f_string(a_towers).c_str());
		auto tower = static_cast<t_pair*>(f_get(a_towers, a_from));
		return f_put(f_put(a_towers, a_from, tower->v_tail), a_to,
			f_new<t_pair>(tower->v_head, f_get(a_towers, a_to))
		);
	});
	std::printf("%s\n", f_string(towers).c_str());
	assert(f_string(towers) == "(() () (a b c d e))");
	return engine.f_exit(0);
}
