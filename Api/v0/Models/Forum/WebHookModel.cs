namespace devanewbot.Api.v0.Models.Forum;

using System.Collections.Generic;
using System.Text.Json.Serialization;

// I used ChatGPT to generate this, I take zero responsibility for this.

public class WebHookModel
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("event")]
    public string Event { get; set; }

    [JsonPropertyName("content_id")]
    public int ContentId { get; set; }

    [JsonPropertyName("data")]
    public Data Data { get; set; }
}

public class Data
{
    [JsonPropertyName("custom_fields")]
    public Dictionary<string, object> CustomFields { get; set; }

    [JsonPropertyName("discussion_open")]
    public bool DiscussionOpen { get; set; }

    [JsonPropertyName("discussion_state")]
    public string DiscussionState { get; set; }

    [JsonPropertyName("discussion_type")]
    public string DiscussionType { get; set; }

    [JsonPropertyName("first_post_id")]
    public int FirstPostId { get; set; }

    [JsonPropertyName("first_post_reaction_score")]
    public int FirstPostReactionScore { get; set; }

    [JsonPropertyName("Forum")]
    public Forum Forum { get; set; }

    [JsonPropertyName("highlighted_post_ids")]
    public List<int> HighlightedPostIds { get; set; }

    [JsonPropertyName("is_first_post_pinned")]
    public bool IsFirstPostPinned { get; set; }

    [JsonPropertyName("is_search_engine_indexable")]
    public bool IsSearchEngineIndexable { get; set; }

    [JsonPropertyName("last_post_date")]
    public long LastPostDate { get; set; }

    [JsonPropertyName("last_post_id")]
    public int LastPostId { get; set; }

    [JsonPropertyName("last_post_user_id")]
    public int LastPostUserId { get; set; }

    [JsonPropertyName("last_post_username")]
    public string LastPostUsername { get; set; }

    [JsonPropertyName("node_id")]
    public int NodeId { get; set; }

    [JsonPropertyName("post_date")]
    public long PostDate { get; set; }

    [JsonPropertyName("prefix_id")]
    public int PrefixId { get; set; }

    [JsonPropertyName("reply_count")]
    public int ReplyCount { get; set; }

    [JsonPropertyName("sticky")]
    public bool Sticky { get; set; }

    [JsonPropertyName("tags")]
    public List<object> Tags { get; set; }

    [JsonPropertyName("thread_id")]
    public int ThreadId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("User")]
    public User User { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("view_count")]
    public int ViewCount { get; set; }

    [JsonPropertyName("view_url")]
    public string? ViewUrl { get; set; }
}

public class Forum
{
    [JsonPropertyName("breadcrumbs")]
    public List<Breadcrumb> Breadcrumbs { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("display_in_list")]
    public bool DisplayInList { get; set; }

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("node_id")]
    public int NodeId { get; set; }

    [JsonPropertyName("node_name")]
    public string NodeName { get; set; }

    [JsonPropertyName("node_type_id")]
    public string NodeTypeId { get; set; }

    [JsonPropertyName("parent_node_id")]
    public int ParentNodeId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("type_data")]
    public TypeData TypeData { get; set; }

    [JsonPropertyName("view_url")]
    public string ViewUrl { get; set; }
}

public class Breadcrumb
{
    [JsonPropertyName("node_id")]
    public int NodeId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("node_type_id")]
    public string NodeTypeId { get; set; }
}

public class TypeData
{
    [JsonPropertyName("allow_posting")]
    public bool AllowPosting { get; set; }

    [JsonPropertyName("can_create_thread")]
    public bool CanCreateThread { get; set; }

    [JsonPropertyName("can_upload_attachment")]
    public bool CanUploadAttachment { get; set; }

    [JsonPropertyName("discussion")]
    public Discussion Discussion { get; set; }

    [JsonPropertyName("discussion_count")]
    public int DiscussionCount { get; set; }

    [JsonPropertyName("forum_type_id")]
    public string ForumTypeId { get; set; }

    [JsonPropertyName("is_unread")]
    public bool IsUnread { get; set; }

    [JsonPropertyName("last_post_date")]
    public long LastPostDate { get; set; }

    [JsonPropertyName("last_post_id")]
    public int LastPostId { get; set; }

    [JsonPropertyName("last_post_username")]
    public string LastPostUsername { get; set; }

    [JsonPropertyName("last_thread_id")]
    public int LastThreadId { get; set; }

    [JsonPropertyName("last_thread_prefix_id")]
    public int LastThreadPrefixId { get; set; }

    [JsonPropertyName("last_thread_title")]
    public string LastThreadTitle { get; set; }

    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    [JsonPropertyName("min_tags")]
    public int MinTags { get; set; }

    [JsonPropertyName("require_prefix")]
    public bool RequirePrefix { get; set; }
}

public class Discussion
{
    [JsonPropertyName("allowed_thread_types")]
    public List<string> AllowedThreadTypes { get; set; }

    [JsonPropertyName("allow_answer_voting")]
    public bool AllowAnswerVoting { get; set; }

    [JsonPropertyName("allow_answer_downvote")]
    public bool AllowAnswerDownvote { get; set; }
}

public class User
{
    [JsonPropertyName("avatar_urls")]
    public AvatarUrls AvatarUrls { get; set; }

    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; }

    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    [JsonPropertyName("profile_banner_urls")]
    public ProfileBannerUrls ProfileBannerUrls { get; set; }

    [JsonPropertyName("question_solution_count")]
    public int QuestionSolutionCount { get; set; }

    [JsonPropertyName("reaction_score")]
    public int ReactionScore { get; set; }

    [JsonPropertyName("register_date")]
    public long RegisterDate { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    [JsonPropertyName("trophy_points")]
    public int TrophyPoints { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("user_title")]
    public string UserTitle { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("view_url")]
    public string ViewUrl { get; set; }

    [JsonPropertyName("vote_score")]
    public int VoteScore { get; set; }
}

public class AvatarUrls
{
    [JsonPropertyName("o")]
    public string O { get; set; }

    [JsonPropertyName("h")]
    public string H { get; set; }

    [JsonPropertyName("l")]
    public string L { get; set; }

    [JsonPropertyName("m")]
    public string M { get; set; }

    [JsonPropertyName("s")]
    public string S { get; set; }
}

public class ProfileBannerUrls
{
    [JsonPropertyName("l")]
    public string L { get; set; }

    [JsonPropertyName("m")]
    public string M { get; set; }
}