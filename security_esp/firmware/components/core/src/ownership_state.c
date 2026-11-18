#include "ownership_state.h"
#include <string.h>

void ownership_state_init(ownership_state_t *state)
{
    memset(state, 0, sizeof(*state));
}

void ownership_state_apply_sync(ownership_state_t *state, uint64_t new_version, bool is_linked)
{
    state->version = new_version;
    state->is_linked = is_linked;
}
