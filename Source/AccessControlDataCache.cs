﻿using AccessControlAdapterSample.AdapterData;

namespace AccessControlAdapterSample
{
	class AccessControlDataCache
	{
		public static AccessControlDataCache Instance { get; private set; }



		public Dictionary<string, UserData> UsersMap { get; set; } = new Dictionary<string, UserData>();

		public Dictionary<string, EventData> EventsMap { get; set; } = new Dictionary<string, EventData>();



		public void SetItems(Dictionary<string, ItemData> newItemsMap, INotificationsManager notificationsManager)
		{
			var itemsToAdd = new List<ItemData>();
			var itemsToRemove = new List<ItemData>();
			var itemsToUpdate = new List<Tuple<ItemData, ItemData>>();
			lock (_itemsLock)
			{
				foreach (var item in _itemsMap.Values)
				{
					if (item.Id == null)
						continue;

					if (newItemsMap.TryGetValue(item.Id, out var updatedItem))
					{
						if (!item.Equals(updatedItem))
							itemsToUpdate.Add(new Tuple<ItemData, ItemData>(item, updatedItem));
					}
					else
						itemsToRemove.Add(item);
				}

				foreach (var item in newItemsMap.Values)
				{
					if (item.Id == null)
						continue;

					if (!_itemsMap.ContainsKey(item.Id))
						itemsToAdd.Add(item);
				}

				foreach (var item in itemsToRemove)
					if (item.Id != null)
						_itemsMap.Remove(item.Id);

				foreach (var item in itemsToAdd)
					if (item.Id != null)
						_itemsMap[item.Id] = item;

				foreach (var item in itemsToUpdate)
					if (item.Item1.Id != null && item.Item2.Id != null)
						_itemsMap[item.Item2.Id] = item.Item2;
			}

			foreach (var item in itemsToRemove)
				if (item.Id != null)
					notificationsManager.PushItemUpdatedNotification(item.Type, item, null);

			foreach (var item in itemsToAdd)
				if (item.Id != null)
					notificationsManager.PushItemUpdatedNotification(item.Type, null, item);


			foreach (var item in itemsToUpdate)
				if (item.Item1.Id != null && item.Item2.Id != null)
					notificationsManager.PushItemUpdatedNotification(item.Item2.Type, item.Item1, item.Item2);
		}

		public void UpdateItem(ItemData updatedItem, INotificationsManager notificationsManager)
		{
			if (updatedItem.Id == null)
				return;

			ItemData? existingItem;
			lock (_itemsLock)
			{
				if (_itemsMap.TryGetValue(updatedItem.Id, out existingItem))
				{
					if (existingItem.Equals(updatedItem))
						return; // Item wasn't changed
				}

				_itemsMap[updatedItem.Id] = updatedItem;
			}

			notificationsManager.PushItemUpdatedNotification(updatedItem.Type, existingItem, updatedItem);
		}

		public void UpdateUser(UserData updatedUser)
		{
			if (updatedUser.Id == null)
				return;

			lock (UsersMap)
			{
				UsersMap[updatedUser.Id] = updatedUser;
			}
		}

		public List<ItemData> GetItems()
		{
			lock (_itemsLock)
			{
				return _itemsMap.Values.ToList();
			}
		}

		public ItemData? GetItem(string? category, string? id)
		{
			if (category == null || id == null)
				return null;

			lock (_itemsLock)
			{
				if (_itemsMap.TryGetValue(id, out var itemData))
				{
					if (itemData.Category != category)
						return null;

					return itemData;
				}
			}

			return null;
		}

		public List<UserData> GetUsers()
		{
			lock (UsersMap)
			{
				return UsersMap.Values.ToList();
			}
		}

		public UserData? GetUser(string? id)
		{
			if (id == null)
				return null;

			lock (UsersMap)
			{
				if (UsersMap.TryGetValue(id, out var userData))
					return userData;
			}

			return null;
		}

		public List<EventData> GetEvents()
		{
			lock (EventsMap)
			{
				return EventsMap.Values.ToList();
			}
		}

		public EventData? GetEvent(string? id)
		{
			if (id == null)
				return null;

			lock (EventsMap)
			{
				if (EventsMap.TryGetValue(id, out var eventData))
					return eventData;
			}

			return null;
		}

		public List<StateData> GetItemsStates()
		{
			var statesList = new List<StateData>();

			foreach (var state in PossibleDoorStates)
				statesList.Add(new StateData { Id = state.Item1, Title = state.Item1, LocalizationId = state.Item2, Category = EntityData.DoorCategoryName });

			foreach (var state in PossibleZoneStates)
				statesList.Add(new StateData { Id = state.Item1, Title = state.Item1, LocalizationId = state.Item2, Category = EntityData.ZoneCategoryName });

			foreach (var state in PossiblePartitionStates)
				statesList.Add(new StateData { Id = state.Item1, Title = state.Item1, LocalizationId = state.Item2, Category = EntityData.PartitionCategoryName });

			foreach (var state in PossibleOutputStates)
				statesList.Add(new StateData { Id = state.Item1, Title = state.Item1, LocalizationId = state.Item2, Category = EntityData.OutputCategoryName });

			return statesList;
		}



		readonly object _itemsLock = new object();
		readonly Dictionary<string, ItemData> _itemsMap = new Dictionary<string, ItemData>();



		static readonly Tuple<string, string>[] PossibleDoorStates =
			[
				new Tuple<string, string>("opened", "#DoorStatusOpen"),
				new Tuple<string, string>("closed", "#DoorStatusClosed"),
				new Tuple<string, string>("unlocked", "#DoorStatusUnlocked"),
				new Tuple<string, string>("locked", "#DoorStatusLocked")
			];

		static readonly Tuple<string, string>[] PossibleZoneStates =
			[
				new Tuple<string, string>("zoneArmed", "#StatusArmed"),
				new Tuple<string, string>("zoneDisarmed", ""),
				new Tuple<string, string>("zoneReady", "#StatusReady"),
				new Tuple<string, string>("zoneUnbypassed", "")
			];

		static readonly Tuple<string, string>[] PossiblePartitionStates =
			[
				new Tuple<string, string>("partitionArmed", "#StatusArmed"),
				new Tuple<string, string>("partitionDisarmed", ""),
				new Tuple<string, string>("partitionInAlarm", "#StatusAlarmed"),
				new Tuple<string, string>("partitionReady", "#StatusReady")
			];

		static readonly Tuple<string, string>[] PossibleOutputStates =
			[
				new Tuple<string, string>("outputActivated", "#StatusActivated"),
				new Tuple<string, string>("outputDeactivated", "#StatusDeactivated")
			];



		static AccessControlDataCache()
		{
			Instance = new AccessControlDataCache();
		}
	}
}
