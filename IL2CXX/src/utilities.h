IL2CXX__PORTABLE__ALWAYS_INLINE inline t_System_2eString* f__new_string(size_t a_length)
{
	t__new<t_System_2eString> p(sizeof(char16_t) * a_length);
	p->v__5fstringLength = a_length;
	(&p->v__5ffirstChar)[a_length] = u'\0';
	return p;
}

inline t_System_2eString* f__new_string(std::u16string_view a_value)
{
	auto p = f__new_string(a_value.size());
	std::memcpy(&p->v__5ffirstChar, a_value.data(), a_value.size() * sizeof(char16_t));
	return p;
}

std::vector<char16_t> f__to_cs16(t_System_2eText_2eStringBuilder* a_p);
void f__from(t_System_2eText_2eStringBuilder* a_p, const char16_t* a_cs);
std::vector<char> f__to_cs(t_System_2eText_2eStringBuilder* a_p);
void f__from(t_System_2eText_2eStringBuilder* a_p, const char* a_cs);

[[noreturn]] void f__throw_argument();
[[noreturn]] void f__throw_argument_null();
[[noreturn]] void f__throw_index_out_of_range();
[[noreturn]] void f__throw_invalid_cast();
[[noreturn]] void f__throw_null_reference();
[[noreturn]] void f__throw_overflow();
