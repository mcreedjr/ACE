using System;
using System.Collections.ObjectModel;
using System.Threading;

using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity.Actions;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        public static TimeSpan PlayerSaveInterval = TimeSpan.FromMinutes(5);

        public DateTime CharacterLastRequestedDatabaseSave { get; protected set; }

        /// <summary>
        /// This variable is set to true when a change is made, and set to false before a save is requested.<para />
        /// The primary use for this is to trigger save on add/modify/remove of properties.
        /// </summary>
        public bool CharacterChangesDetected { get; set; }

        /// <summary>
        /// Best practice says you should use this lock any time you read/write the Character.<para />
        /// <para />
        /// For absolute maximum performance, if you're willing to assume (and risk) the following:<para />
        ///  - that the character in the database will not be modified (in a way that adds or removes properties) outside of ACE while ACE is running with a reference to that character<para />
        ///  - that the character will only be read/modified by a single thread in ACE<para />
        /// You can remove the lock usage for any Get/GetAll Property functions. You would simply use it for Set/Remove Property functions because each of these could end up adding/removing to the collections.<para />
        /// The critical thing is that the collections are not added to or removed from while Entity Framework is iterating over them.<para />
        /// Mag-nus 2018-08-19
        /// </summary>
        public readonly ReaderWriterLockSlim CharacterDatabaseLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets the ActionChain to save a character
        /// </summary>
        public ActionChain GetSaveChain()
        {
            return new ActionChain(this, SavePlayer);
        }

        /// <summary>
        /// Creates and Enqueues an ActionChain to save a character
        /// </summary>
        public void EnqueueSaveChain()
        {
            GetSaveChain().EnqueueChain();
        }

        /// <summary>
        /// Internal save character functionality<para  />
        /// Saves the character to the persistent database. Includes Stats, Position, Skills, etc.<para />
        /// Will also save any possessions that are marked with ChangesDetected.
        /// </summary>
        private void SavePlayer()
        {
            if (CharacterChangesDetected)
                SaveCharacterToDatabase();

            var biotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();

            SaveBiotaToDatabase(false);
            biotas.Add((Biota, BiotaDatabaseLock));

            var allPosessions = GetAllPossessions();

            foreach (var possession in allPosessions)
            {
                if (possession.ChangesDetected)
                {
                    possession.SaveBiotaToDatabase(false);
                    biotas.Add((possession.Biota, possession.BiotaDatabaseLock));
                }
            }

            var requestedTime = DateTime.UtcNow;

            DatabaseManager.Shard.SaveBiotas(biotas, result => log.Debug($"{Session.Player.Name} has been saved. It took {(DateTime.UtcNow - requestedTime).TotalMilliseconds:N0} ms to process the request."));
        }

        public void SaveCharacterToDatabase()
        {
            // Make sure our IsPlussed value is up to date
            bool isPlussed = (GetProperty(PropertyBool.IsAdmin) ?? false) || (GetProperty(PropertyBool.IsArch) ?? false) || (GetProperty(PropertyBool.IsPsr) ?? false) || (GetProperty(PropertyBool.IsSentinel) ?? false);

            if (WeenieType == WeenieType.Admin || WeenieType == WeenieType.Sentinel)
                isPlussed = true;

            Character.IsPlussed = isPlussed;

            CharacterLastRequestedDatabaseSave = DateTime.UtcNow;
            CharacterChangesDetected = false;

            DatabaseManager.Shard.SaveCharacter(Character, CharacterDatabaseLock, null);
        }

        /// <summary>
        /// This will set the LastRequestedDatabaseSave to UtcNow and ChangesDetected to false.<para />
        /// If enqueueSave is set to true, DatabaseManager.Shard.SaveBiota() will be called for the biota.<para />
        /// Set enqueueSave to false if you want to perform all the normal routines for a save but not the actual save. This is useful if you're going to collect biotas in bulk for bulk saving.
        /// </summary>
        public override void SaveBiotaToDatabase(bool enqueueSave = true)
        {
            // Save the current position to persistent storage, only during the server update interval
            SetPhysicalCharacterPosition();

            base.SaveBiotaToDatabase(enqueueSave);
        }
    }
}
