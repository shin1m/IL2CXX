namespace
{

template<typename T_push>
void f__to_u16(const char* a_first, const char* a_last, T_push a_push)
{
	std::mbstate_t state{};
	char16_t c;
	while (a_first < a_last) {
		auto n = std::mbrtoc16(&c, a_first, a_last - a_first, &state);
		switch (n) {
		case size_t(-3):
			a_push(c);
			break;
		case size_t(-2):
			a_first = a_last;
			break;
		case size_t(-1):
			++a_first;
			break;
		case 0:
			a_push(u'\0');
			++a_first;
			break;
		default:
			a_push(c);
			a_first += n;
			break;
		}
	}
	if (std::mbrtoc16(&c, a_first, 0, &state) == size_t(-3)) a_push(c);
}

}

std::u16string f__u16string(std::string_view a_x)
{
	std::vector<char16_t> cs;
	f__to_u16(a_x.data(), a_x.data() + a_x.size(), [&](auto c)
	{
		cs.push_back(c);
	});
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

t_System_2eString* f__new_string(std::string_view a_x)
{
	auto p = f__new_string(a_x.size());
	auto q = &p->v__5ffirstChar;
	f__to_u16(a_x.data(), a_x.data() + a_x.size(), [&](auto c)
	{
		*q++ = c;
	});
	return p;
}

char* f__copy_to(const t_System_2eString* a_x, char* a_p, char* a_q)
{
	std::mbstate_t state{};
	char mb[MB_LEN_MAX];
	for (auto c : std::u16string_view{&a_x->v__5ffirstChar, static_cast<size_t>(a_x->v__5fstringLength + 1)}) {
		auto n = std::c16rtomb(mb, c, &state);
		if (n == size_t(-1)) continue;
		if (a_p + n >= a_q) {
			*a_p = '\0';
			return a_p;
		}
		a_p = std::copy_n(mb, n, a_p);
	}
	return a_p;
}

std::vector<char16_t> f__to_cs16(t_System_2eText_2eStringBuilder* a_p)
{
	std::vector<char16_t> cs(a_p->v_m_5fChunkOffset + a_p->v_m_5fChunkChars->v__length + 1);
	cs[a_p->v_m_5fChunkOffset + a_p->v_m_5fChunkLength] = u'\0';
	do {
		std::copy_n(a_p->v_m_5fChunkChars->f_data(), a_p->v_m_5fChunkLength, cs.data() + a_p->v_m_5fChunkOffset);
		a_p = a_p->v_m_5fChunkPrevious;
	} while (a_p);
	return cs;
}

void f__from(t_System_2eText_2eStringBuilder* a_p, const char16_t* a_cs)
{
	auto p = a_cs;
	while (*p) ++p;
	size_t n = p - a_cs;
	while (true) {
		auto m = a_p->v_m_5fChunkChars->v__length;
		if (n <= m) break;
		n -= m;
		std::copy_n(a_cs + n, m, a_p->v_m_5fChunkChars->f_data());
		a_p->v_m_5fChunkOffset = n;
		a_p->v_m_5fChunkLength = m;
		a_p = a_p->v_m_5fChunkPrevious;
	}
	std::copy_n(a_cs, n, a_p->v_m_5fChunkChars->f_data());
	a_p->v_m_5fChunkOffset = 0;
	a_p->v_m_5fChunkLength = n;
	a_p->v_m_5fChunkPrevious = nullptr;
}

std::vector<char> f__to_cs(t_System_2eText_2eStringBuilder* a_p)
{
	std::vector<char> cs((a_p->v_m_5fChunkOffset + a_p->v_m_5fChunkChars->v__length) * MB_LEN_MAX + 1);
	std::vector<t_root<t_slot_of<t_System_2eText_2eStringBuilder>>> chunks;
	do {
		chunks.push_back(a_p);
		a_p = a_p->v_m_5fChunkPrevious;
	} while (a_p);
	std::mbstate_t state{};
	char mb[MB_LEN_MAX];
	auto p = cs.data();
	for (auto i = chunks.rbegin(); i != chunks.rend(); ++i) {
		size_t m = (*i)->v_m_5fChunkLength;
		auto q = (*i)->v_m_5fChunkChars->f_data();
		for (size_t j = 0; j < m; ++j) {
			auto n = std::c16rtomb(mb, q[j], &state);
			if (n != size_t(-1)) p = std::copy_n(mb, n, p);
		}
	}
	auto n = std::c16rtomb(mb, u'\0', &state);
	if (n != size_t(-1) && n > 1) std::copy_n(mb, n - 1, p);
	return cs;
}

void f__from(t_System_2eText_2eStringBuilder* a_p, const char* a_cs)
{
	std::vector<t_root<t_slot_of<t_System_2eText_2eStringBuilder>>> chunks;
	do {
		a_p->v_m_5fChunkLength = 0;
		chunks.push_back(a_p);
		a_p = a_p->v_m_5fChunkPrevious;
	} while (a_p);
	auto i = chunks.rbegin();
	auto p = *i;
	p->v_m_5fChunkOffset = 0;
	f__to_u16(a_cs, a_cs + std::strlen(a_cs), [&](auto c)
	{
		p->v_m_5fChunkChars->f_data()[p->v_m_5fChunkLength] = c;
		if (++p->v_m_5fChunkLength >= p->v_m_5fChunkChars->v__length) {
			auto n = p->v_m_5fChunkOffset + p->v_m_5fChunkLength;
			p = *++i;
			p->v_m_5fChunkOffset = n;
		}
	});
	auto n = p->v_m_5fChunkOffset + p->v_m_5fChunkLength;
	while (++i != chunks.rend()) (*i)->v_m_5fChunkOffset = n;
}

namespace
{

std::regex v__type_prefix{"^(:?[^,\\[\\\\\\]`]|\\\\.)+(:?(`\\d+)|\\[[^\\]]*\\])?"};

inline std::string_view f__sv(std::string_view::const_iterator a_first, std::string_view::const_iterator a_last)
{
	return {&*a_first, static_cast<size_t>(a_last - a_first)};
}

std::pair<std::string_view::const_iterator, std::string_view::const_iterator> f__match_type(std::string_view a_x, std::string_view a_y)
{
	auto fail = [&]
	{
		return std::make_pair(a_x.end(), a_y.begin());
	};
	std::match_results<std::string_view::const_iterator> match;
	if (!std::regex_search(a_x.begin(), a_x.end(), match, v__type_prefix)) return fail();
	auto n = match.length(0);
	if (a_x.substr(0, n) != a_y.substr(0, n)) return fail();
	auto i = a_x.begin() + n;
	auto j = a_y.begin() + n;
	if (match.length(1) > 0 && i != a_x.end() && *i == u'[') {
		if (j == a_y.end() || *j != u'[') return fail();
		auto equals = [&](auto i, auto j, char16_t c)
		{
			return i != a_x.end() && *i == c && j != a_y.end() && *j == c;
		};
		do {
			if (!equals(++i, ++j, u'[')) return fail();
			auto x = f__match_type(f__sv(++i, a_x.end()), f__sv(++j, a_y.end()));
			i = x.first;
			j = x.second;
			if (!equals(i, j, u']')) return fail();
		} while (equals(++i, ++j, u','));
		if (!equals(i, j, u']')) return fail();
		++i;
		++j;
	}
	for (; j != a_y.end() && *j != u']'; ++i, ++j) if (i == a_x.end() || *i != *j) return fail();
	return {std::find(i, a_x.end(), u']'), j};
}

}

t__type* f__find_type(const std::map<std::string_view, t__type*>& a_name_to_type, std::u16string_view a_name)
{
	auto s = f__string(a_name);
	std::string_view name = s;
	std::match_results<std::string_view::const_iterator> match;
	if (!std::regex_search(name.begin(), name.end(), match, v__type_prefix)) return nullptr;
	auto prefix = f__sv(match[0].first, match[0].second);
	for (auto i = a_name_to_type.lower_bound(prefix); i != a_name_to_type.end(); ++i) {
		if (i->first.substr(0, prefix.size()) != prefix) break;
		if (f__match_type(i->first, name).second == name.end()) return i->second;
	}
	return nullptr;
}

RECYCLONE__THREAD int32_t v_last_unmanaged_error;
