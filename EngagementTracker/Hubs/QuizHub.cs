using Microsoft.AspNetCore.SignalR;

namespace EngagementTracker.Hubs;

public class QuizHub : Hub
{
    public async Task SendQuestion(string question, string[] options, int timeSeconds)
    {
        await Clients.All.SendAsync("ReceiveQuestion", question, options, timeSeconds);
    }

    public async Task SubmitAnswer(string studentUid, string studentName, int answerIndex)
    {
        await Clients.All.SendAsync("ReceiveAnswer", studentUid, studentName, answerIndex);
    }

    public async Task StudentCheated(string studentUid, string studentName)
    {
        await Clients.All.SendAsync("StudentCheatedNotify", studentUid, studentName);
    }

    public async Task RevealAnswer(int correctIndex)
    {
        await Clients.All.SendAsync("AnswerRevealed", correctIndex);
    }

    public async Task EndQuiz(string resultsJson)
    {
        await Clients.All.SendAsync("QuizEnded", resultsJson);
    }

    public async Task NextQuestion()
    {
        await Clients.All.SendAsync("QuestionReset");
    }
}