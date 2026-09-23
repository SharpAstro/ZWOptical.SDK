/*
 * The one symbol ZWO's EFW static library cannot find in a static C runtime.
 *
 * Every object in EFW_filter-static.lib links against LIBCMT except one, which was compiled for the
 * DLL runtime and so calls strncpy through an import slot (__imp_strncpy) rather than directly.
 * A static-runtime link has no import slots to offer, so this supplies that one slot, pointing at
 * the statically linked strncpy. Nothing else in the library needed it: without this file the link
 * fails with exactly one unresolved external, LNK2001 __imp_strncpy.
 */
#include <string.h>

char *(__cdecl *__imp_strncpy)(char *, const char *, size_t) = strncpy;
