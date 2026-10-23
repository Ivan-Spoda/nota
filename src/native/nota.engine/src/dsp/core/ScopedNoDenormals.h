// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// ScopedNoDenormals — single RAII FTZ/DAZ guard for the whole engine (AR-6).
// Replaces the scattered copies in Engine_Render.cpp, Consort.h, Keys.h and
// Pentad.h. Construct on the audio thread and on every AudioThreadPool worker.

#pragma once

#if defined(__x86_64__) || defined(_M_X64) || defined(__i386__) || defined(_M_IX86)
#include <xmmintrin.h>
#elif defined(_M_ARM64) && defined(_MSC_VER) && !defined(__clang__)
#include <intrin.h>
#endif
#include <cstdint>

namespace nota {
namespace dsp {

class ScopedNoDenormals {
  public:
    ScopedNoDenormals() noexcept { enter(); }
    ~ScopedNoDenormals() noexcept { leave(); }

    ScopedNoDenormals(const ScopedNoDenormals&) = delete;
    ScopedNoDenormals& operator=(const ScopedNoDenormals&) = delete;

  private:
#if defined(__aarch64__) || defined(_M_ARM64)
    uint64_t saved_ = 0;

    // FPCR.FZ bit 24: flush-to-zero. ARM64 has no DAZ bit; FZ covers it.
    void enter() noexcept {
        saved_ = readFpcr();
        writeFpcr(saved_ | (1ULL << 24));
    }
    void leave() noexcept { writeFpcr(saved_); }

#if defined(_MSC_VER) && !defined(__clang__)
    // MSVC ARM64 has no GCC-style inline asm; FPCR = ARM64_SYSREG(3, 3, 4, 4, 0).
    static constexpr int kFpcr = 0x5A20;
    static uint64_t readFpcr() noexcept { return static_cast<uint64_t>(_ReadStatusReg(kFpcr)); }
    static void writeFpcr(uint64_t v) noexcept { _WriteStatusReg(kFpcr, static_cast<__int64>(v)); }
#else
    static uint64_t readFpcr() noexcept {
        uint64_t v;
        __asm__ volatile("mrs %0, fpcr" : "=r"(v));
        return v;
    }
    static void writeFpcr(uint64_t v) noexcept { __asm__ volatile("msr fpcr, %0" : : "r"(v)); }
#endif
#elif defined(__x86_64__) || defined(_M_X64) || defined(__i386__) || defined(_M_IX86)
    unsigned saved_ = 0;

    void enter() noexcept {
        saved_ = _mm_getcsr();
        _mm_setcsr(saved_ | 0x8040); // FTZ (bit 15) | DAZ (bit 6)
    }
    void leave() noexcept { _mm_setcsr(saved_); }
#else
    void enter() noexcept {}
    void leave() noexcept {}
#endif
};

} // namespace dsp
} // namespace nota
