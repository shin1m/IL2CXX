#include <vector>
#include "pair.cc"
#include "thread.cc"

int main(int argc, char* argv[])
{
	::t_engine::t_options options;
	options.v_verbose = options.v_verify = true;
	::t_engine engine(options);
	t_pair* p = nullptr;
	std::vector<t_thread*> ts;
	for (size_t i = 0; i < 10; ++i) ts.push_back(engine.f_start_thread([&p, i]
	{
		std::printf("%d\n", i);
		p = f_cons(f_symbol(std::to_string(i)), p);
	}));
	for (auto t : ts) engine.f_join(t);
	std::printf("%s\n", f_string(p).c_str());
	return engine.f_exit(0);
}
