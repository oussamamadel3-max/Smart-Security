#include "command_dedup.h"
#include <string.h>

void command_dedup_init(command_dedup_t *dedup)
{
    memset(dedup, 0, sizeof(*dedup));
}

bool command_dedup_check_and_record(command_dedup_t *dedup, const char *command_id)
{
    for (int i = 0; i < COMMAND_DEDUP_CAPACITY; i++) {
        if (dedup->slot_filled[i] && strncmp(dedup->ids[i], command_id, COMMAND_DEDUP_ID_MAX_LEN) == 0) {
            return true;
        }
    }
    strncpy(dedup->ids[dedup->next_slot], command_id, COMMAND_DEDUP_ID_MAX_LEN);
    dedup->ids[dedup->next_slot][COMMAND_DEDUP_ID_MAX_LEN] = '\0';
    dedup->slot_filled[dedup->next_slot] = true;
    dedup->next_slot = (uint8_t)((dedup->next_slot + 1) % COMMAND_DEDUP_CAPACITY);
    return false;
}
