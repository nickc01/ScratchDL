using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader.Featured
{
    public record class FeaturedProject(
        string thumbnail_url,
        string title,
        string creator,
        string type,
        long id,
        long love_count
        );

    public record class MostRemixedProject(
        string thumbnail_url,
        string title,
        string creator,
        string type,
        long id,
        long love_count,
        long remixers_count
        ) : FeaturedProject(thumbnail_url, title, creator, type, id, love_count);

    public record class ScratchDesignStudioProject(
        string thumbnail_url,
        string title,
        string creator,
        string type,
        long id,
        long love_count,
        long remixers_count,
        long gallery_id,
        string gallery_title
        ) : MostRemixedProject(thumbnail_url,title,creator,type,id,love_count,remixers_count);

    public record class CuratorTopProject(
        string thumbnail_url,
        string title,
        string creator,
        string type,
        long id,
        long love_count,
        string curator_name
        ) : FeaturedProject(thumbnail_url, title, creator, type, id, love_count);

    public record class CommunityFeaturedStudio
    (
        string thumbnail_url,
        string type,
        long id,
        string title
    );
}
