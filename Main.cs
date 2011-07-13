using System;
using System.Collections;
using Gnome.Keyring;

namespace GnomeKeyringMerger
{
	class MainClass
	{	
		public static void Main (string[] args)
		{
			Console.WriteLine ("== GnomeKeyring Merger 0.1\n");
			
			String to_keyring = "";
			while(to_keyring == "") {
				Console.WriteLine ("\nMERGE TO KEYRING:");
				to_keyring = SelectKeyring();
			}
			
			String from_keyring = "";
			while(from_keyring == "") {
				Console.WriteLine ("\nMERGE FROM KEYRING:");
				from_keyring = SelectKeyring();
			}
			
			int i = 0;
			foreach (int id in Ring.ListItemIDs (from_keyring)) {
				try {
					//if(i > 5) {	break; } i++;
					
					ItemData from_info = Ring.GetItemInfo (from_keyring, id);
					Hashtable from_attr = Ring.GetItemAttributes (from_keyring, id);
					
					//Console.WriteLine ("{0}", from_info.Attributes["name"]);
					
					//DumpItemInfo(from_info, from_attr);
					
					ItemData[] matches = Ring.Find(from_info.Type, from_attr);
					
					int matches_i = 0;
					foreach(ItemData match in matches) {
						if ( match.Keyring != to_keyring ) continue;
						matches_i++;
					}
					
					if(matches_i > 1) {
						Console.WriteLine ("GOT MULTIPLE ITEMS: {0}", from_info.Attributes["name"]);
					} else if(matches_i > 0) {
						//Console.WriteLine (" Found match, is updating needed?");
						
						foreach(ItemData to_info in matches) {
							if ( to_info.Keyring != to_keyring ) continue;
							
							if(to_info.Secret != from_info.Secret) {
								Console.WriteLine ("Do you want to replace THIS?:");
								Hashtable to_attr = Ring.GetItemAttributes(to_keyring, to_info.ItemID);
								DumpItemInfo(to_info, to_attr);
								
								Console.WriteLine ("with THIS?:");
								DumpItemInfo(from_info, from_attr);
								
								String answer = SelectYesNo();
								if(answer == "y") {
									Console.WriteLine (" Updating item");
									// TODO: Ring.SetItemInfo(to_keyring, to_info.ItemID, from_info.Type, from_info.Attributes["name"], from_info.Secret);
								} else {
									Console.WriteLine (" Skipping item");
								}
							} else {
								//Console.WriteLine ("  Nope, secret is same");
							}
						}
					} else {
						Console.WriteLine ("Do you want to add THIS?:");
						DumpItemInfo(from_info, from_attr);
						
						String answer = SelectYesNo();
						if(answer == "y") {
							Console.WriteLine ("Adding item");
							Ring.CreateItem(to_keyring, from_info.Type, from_info.Attributes["name"].ToString(), from_attr, from_info.Secret, false);
						} else {
							Console.WriteLine ("Skipping item");
						}
					}
				} catch( System.ArgumentException e ) {
					Console.WriteLine("\nWorking with {0} ID# {1}\nGot thrown an error: {0}\n", from_keyring, id, e.Message);
					//throw e;
				}
			}
			
			Console.WriteLine ("Do you want to remove the keyring: {0}?:", from_keyring);
			
			String delete_keyring = SelectYesNo();
			if(delete_keyring == "y") {
				Console.WriteLine ("Removing keyring");
				//Ring.DeleteKeyring(from_keyring);
			} else {
				Console.WriteLine ("Keeping keyring");
			}
		}
		
		protected static void DumpItemInfo(ItemData from_info, Hashtable from_attr) {
			Console.WriteLine (" ID: {0}", from_info.ItemID);
			foreach (string key in from_info.Attributes.Keys) {
				Console.WriteLine ("      {0} = {1}", key, from_info.Attributes[key]);
			}
			foreach (string key in from_attr.Keys) {
				Console.WriteLine ("      {0} = {1}", key, from_attr[key]);
			}
			Console.WriteLine ("      Secret = {0}", from_info.Secret);
		}
		
		protected static String SelectKeyring () {
			
			String[] keyrings = Ring.GetKeyrings ();
			
			for (int i = 0; i < keyrings.Length; i++) {
				Console.WriteLine ("["+i+"] "+keyrings[i]);
			}
			
			Console.Write(": ");
			
			try {
				Int16 num = Convert.ToInt16(Console.ReadLine());
				
				if(num < 0 || num >= keyrings.Length) {
					Console.WriteLine ("ERROR: Number not available");
					return "";
				}
				
				return keyrings[num];
			} catch ( System.FormatException e ) {
				Console.WriteLine ("ERROR: Input was not a number");
			}
			
			return "";
		}
		
		protected static String SelectYesNo () {
			
			String answer = "";
			
			while(answer == "") {
				Console.Write("(y or n): ");
			
				String c = Console.ReadLine();
				
				if(c == "y" || c == "n") {
					answer = c;
				} else {
					Console.WriteLine ("ERROR: Answer must be y or n");
				}
			}
			
			return answer;
		}
	}
}

