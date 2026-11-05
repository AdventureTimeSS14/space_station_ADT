using System.Linq;
using Content.Shared.ADT.Polls;

namespace Content.Server.ADT.Polls;

public static class PollValidation
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 4000;
    public const int MaxQuestionLength = 500;
    public const int MaxQuestions = 50;
    public const int MaxOptions = 20;
    public const int MaxOptionLength = 200;
    public const int MaxRatingSpan = 10;

    public static string? ValidatePoll(PollData poll)
    {
        poll.Title = poll.Title.Trim();
        poll.Description = poll.Description.Trim();

        if (poll.Title.Length == 0)
            return "adt-poll-error-no-title";

        if (poll.Title.Length > MaxTitleLength || poll.Description.Length > MaxDescriptionLength)
            return "adt-poll-error-too-long";

        if (poll.Questions.Count == 0)
            return "adt-poll-error-no-questions";

        if (poll.Questions.Count > MaxQuestions)
            return "adt-poll-error-too-many-questions";

        foreach (var question in poll.Questions)
        {
            question.Text = question.Text.Trim();

            if (question.Text.Length == 0 || question.Text.Length > MaxQuestionLength)
                return "adt-poll-error-question-text";

            switch (question.Type)
            {
                case PollQuestionType.Single:
                case PollQuestionType.Multiple:
                    question.Options = question.Options
                        .Select(o => o.Trim())
                        .Where(o => o.Length > 0)
                        .ToList();

                    if (question.Options.Count < 2 || question.Options.Count > MaxOptions)
                        return "adt-poll-error-options-count";

                    if (question.Options.Any(o => o.Length > MaxOptionLength))
                        return "adt-poll-error-too-long";

                    break;

                case PollQuestionType.Rating:
                    question.Options.Clear();

                    if (question.RatingMin >= question.RatingMax
                        || question.RatingMax - question.RatingMin > MaxRatingSpan)
                    {
                        return "adt-poll-error-rating-range";
                    }

                    break;

                case PollQuestionType.Text:
                    question.Options.Clear();
                    break;

                default:
                    return "adt-poll-error-question-type";
            }
        }

        if (poll.Filters.MinPlaytimeHours < 0
            || poll.Filters.MaxPlaytimeHours < 0
            || poll.Filters.TrackerMinHours < 0
            || poll.Filters.TrackerMaxHours < 0)
        {
            return "adt-poll-error-negative-hours";
        }

        if (string.IsNullOrWhiteSpace(poll.Filters.Tracker))
        {
            poll.Filters.Tracker = null;
            poll.Filters.TrackerMinHours = null;
            poll.Filters.TrackerMaxHours = null;
        }

        poll.Servers.RemoveWhere(string.IsNullOrWhiteSpace);

        if (poll.EndsAt != null)
            poll.EndsAt = DateTime.SpecifyKind(poll.EndsAt.Value, DateTimeKind.Utc);

        return null;
    }

    public static string? ValidateAnswers(PollData poll, List<PollAnswer> answers, int textMaxLength, out List<PollAnswer> clean)
    {
        clean = new List<PollAnswer>(poll.Questions.Count);

        if (answers.Count != poll.Questions.Count)
            return "adt-poll-error-answers-count";

        for (var i = 0; i < poll.Questions.Count; i++)
        {
            var question = poll.Questions[i];
            var answer = answers[i];
            var result = new PollAnswer();

            switch (question.Type)
            {
                case PollQuestionType.Single:
                case PollQuestionType.Multiple:
                {
                    var options = answer.Options?.Distinct().ToList() ?? new List<int>();

                    if (options.Any(o => o < 0 || o >= question.Options.Count))
                        return "adt-poll-error-bad-answer";

                    if (question.Type == PollQuestionType.Single && options.Count > 1)
                        return "adt-poll-error-bad-answer";

                    if (options.Count > 0)
                    {
                        options.Sort();
                        result.Options = options;
                    }

                    break;
                }

                case PollQuestionType.Rating:
                    if (answer.Rating is { } rating)
                    {
                        if (rating < question.RatingMin || rating > question.RatingMax)
                            return "adt-poll-error-bad-answer";

                        result.Rating = rating;
                    }

                    break;

                case PollQuestionType.Text:
                {
                    var text = answer.Text?.Trim() ?? string.Empty;

                    if (text.Length > textMaxLength)
                        return "adt-poll-error-text-too-long";

                    if (text.Length > 0)
                        result.Text = text;

                    break;
                }
            }

            if (question.Required && result.IsEmpty())
                return "adt-poll-error-required";

            clean.Add(result);
        }

        return null;
    }
}
