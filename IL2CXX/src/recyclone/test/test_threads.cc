#include "thread.h"
#include "pair.h"

int main(int argc, char* argv[])
{
	t_engine<t_type>::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine_with_threads engine(options);
	t_pair* p = nullptr;
	::t_thread* ts[10];
	for (size_t i = 0; i < 10; ++i) ts[i] = engine.f_start_thread([&p, i]
	{
		std::printf("%d\n", i);
		for (size_t j = 0; j < 100; ++j)
			p = f_new<t_pair>(f_new<t_symbol>(std::to_string(i)), p);
	});
	for (auto t : ts) engine.f_join(t);
	std::printf("%s\n", f_string(p).c_str());
	return engine.f_exit(0);
}
