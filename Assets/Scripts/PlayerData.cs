public struct PlayerData
{
    public string PlayerName {  get;  private set; }

    public PlayerData(string name)
    {
        PlayerName = name;
    }
}
