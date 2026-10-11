# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
#
# Encoder-only LAME 3.100 build for Nota's MP3 export. Sources are an unmodified
# snapshot of the upstream 3.100 tarball (see README.nota.md); only the build
# wiring is ours: libmp3lame + the mpglib decoder it depends on, pure C (no nasm,
# no SSE intrinsics), compiled as a private static library linked into nota_engine.
# License: LGPL-2.0-or-later — LICENSES/third-party.md note 7.

set(LAME_ROOT ${CMAKE_CURRENT_SOURCE_DIR})
set(LAME_SOURCES
    ${LAME_ROOT}/libmp3lame/bitstream.c
    ${LAME_ROOT}/libmp3lame/encoder.c
    ${LAME_ROOT}/libmp3lame/fft.c
    ${LAME_ROOT}/libmp3lame/gain_analysis.c
    ${LAME_ROOT}/libmp3lame/id3tag.c
    ${LAME_ROOT}/libmp3lame/lame.c
    ${LAME_ROOT}/libmp3lame/mpglib_interface.c
    ${LAME_ROOT}/libmp3lame/newmdct.c
    ${LAME_ROOT}/libmp3lame/presets.c
    ${LAME_ROOT}/libmp3lame/psymodel.c
    ${LAME_ROOT}/libmp3lame/quantize.c
    ${LAME_ROOT}/libmp3lame/quantize_pvt.c
    ${LAME_ROOT}/libmp3lame/reservoir.c
    ${LAME_ROOT}/libmp3lame/set_get.c
    ${LAME_ROOT}/libmp3lame/tables.c
    ${LAME_ROOT}/libmp3lame/takehiro.c
    ${LAME_ROOT}/libmp3lame/util.c
    ${LAME_ROOT}/libmp3lame/vbrquantize.c
    ${LAME_ROOT}/libmp3lame/version.c
    ${LAME_ROOT}/libmp3lame/VbrTag.c
    # mpglib decoder: linked because lame.c drives the hip_* API in
    # mpglib_interface.c unconditionally (HAVE_MPGLIB in config.h).
    ${LAME_ROOT}/mpglib/common.c
    ${LAME_ROOT}/mpglib/dct64_i386.c
    ${LAME_ROOT}/mpglib/decode_i386.c
    ${LAME_ROOT}/mpglib/interface.c
    ${LAME_ROOT}/mpglib/layer1.c
    ${LAME_ROOT}/mpglib/layer2.c
    ${LAME_ROOT}/mpglib/layer3.c
    ${LAME_ROOT}/mpglib/tabinit.c
)

add_library(lame STATIC ${LAME_SOURCES})
target_include_directories(lame
    PUBLIC  ${LAME_ROOT}/include
    PRIVATE ${LAME_ROOT}           # config.h (ours, one level up from libmp3lame/)
    PRIVATE ${LAME_ROOT}/libmp3lame
    PRIVATE ${LAME_ROOT}/mpglib
)
target_compile_definitions(lame PRIVATE HAVE_CONFIG_H)
