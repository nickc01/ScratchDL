﻿namespace ScratchDL
{
    public record class StudioStatus
    (
        DateTime datetime_created,
        string id,
        long actor_id,
        long project_id,
        string project_title,
        string type,
        string actor_username
    );
}
