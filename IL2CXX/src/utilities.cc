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

std::vector<char16_t> f__to_cs16(t_System_2eText_2eStringBuilder* a_p)
{
	std::vector<char16_t> cs(a_p->v_m_5fChunkOffset + a_p->v_m_5fChunkChars->v__length + 1);
	cs[a_p->v_m_5fChunkOffset + a_p->v_m_5fChunkLength] = u'\0';
	do {
		std::copy_n(a_p->v_m_5fChunkChars->f__data(), a_p->v_m_5fChunkLength, cs.data() + a_p->v_m_5fChunkOffset);
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
		std::copy_n(a_cs + n, m, a_p->v_m_5fChunkChars->f__data());
		a_p->v_m_5fChunkOffset = n;
		a_p->v_m_5fChunkLength = m;
		a_p = a_p->v_m_5fChunkPrevious;
	}
	std::copy_n(a_cs, n, a_p->v_m_5fChunkChars->f__data());
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
		auto q = (*i)->v_m_5fChunkChars->f__data();
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
		p->v_m_5fChunkChars->f__data()[p->v_m_5fChunkLength] = c;
		if (++p->v_m_5fChunkLength >= p->v_m_5fChunkChars->v__length) {
			auto n = p->v_m_5fChunkOffset + p->v_m_5fChunkLength;
			p = *++i;
			p->v_m_5fChunkOffset = n;
		}
	});
	auto n = p->v_m_5fChunkOffset + p->v_m_5fChunkLength;
	while (++i != chunks.rend()) (*i)->v_m_5fChunkOffset = n;
}

void f__throw_argument()
{
	throw std::runtime_error("ArgumentException");
}

void f__throw_argument_null()
{
	throw std::runtime_error("ArgumentNullException");
}

void f__throw_index_out_of_range()
{
	throw std::runtime_error("IndexOutOfRangeException");
}

void f__throw_invalid_cast()
{
	throw std::runtime_error("InvalidCastException");
}

void f__throw_null_reference()
{
	throw std::runtime_error("NullReferenceException");
}

void f__throw_overflow()
{
	throw std::runtime_error("OverflowException");
}
