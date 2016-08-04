namespace ScreenMgr {
    // Used to generate unique ids
    public class UniqueID<T> {
        private static int currentUID = 0;

        public static int NextUID {
            get {
                currentUID++;
                return currentUID;
            }
        }
    }
}