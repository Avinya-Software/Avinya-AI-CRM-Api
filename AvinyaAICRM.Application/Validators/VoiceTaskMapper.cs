using AvinyaAICRM.Application.DTOs.Tasks;

namespace AvinyaAICRM.Application.Validators
{
    public static class VoiceTaskMapper
    {
        public static CreateTaskDto ToCreateTaskDto(string text)
        {
            return new CreateTaskDto
            {
                Title = text,
                Description = "Created via voice",
                Notes = null,

                ListId = 0,              // default list
                IsRecurring = false,
                RecurrenceRule = null,

                DueDateTime = null,
                ReminderAt = null,
                ReminderChannel = null
            };
        }
    }

}
