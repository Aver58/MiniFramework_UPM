public enum Module {
    UI = 1000,

}

public class EventConstantId {
    public const int OnTestEvent = 1;
    public const int OnInventoryItemDataChange = 2;
    public const int OnLevelUpCurrentPet = 3;

    public const ushort UI_Start = (ushort)Module.UI;

    public const ushort UI_End = UI_Start + 1000;

}