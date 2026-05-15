namespace DinoGrow.Core.Stage
{
    public sealed class GameStateController
    {
        public GameState State { get; private set; } = GameState.Ready;

        public bool IsPlaying => State == GameState.Playing;

        public void StartGame()
        {
            State = GameState.Playing;
        }

        public void GameOver()
        {
            State = GameState.GameOver;
        }

        public void Clear()
        {
            State = GameState.Clear;
        }

        public void Reset()
        {
            State = GameState.Ready;
        }
    }
}
