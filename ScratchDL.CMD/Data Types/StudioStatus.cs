﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader
{
    public record class StudioStatus
    (
        string datetime_created,
        string id,
        long actor_id,
        long project_id,
        string project_title,
        string type,
        string actor_username
    );
}
