using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scratch_Downloader.Featured
{
    public record class FeaturedData(
        List<FeaturedProject> community_newest_projects,
        List<MostRemixedProject> community_most_remixed_projects,
        List<ScratchDesignStudioProject> scratch_design_studio,
        List<CuratorTopProject> curator_top_projects,
        List<CommunityFeaturedStudio> community_featured_studios,
        List<FeaturedProject> community_most_loved_projects,
        List<FeaturedProject> community_featured_projects
        );
}
