using System;
using System.Collections;
using Gnome.Keyring;

namespace GnomeKeyringMerger
{
	class MainClass
	{	
		public static void Main( string[] args )
		{
			Console.WriteLine( "== GnomeKeyring Merger 0.1\n" );
			
			Console.WriteLine( "\nMERGE TO KEYRING:" );
			String to_keyring = SelectKeyring();
			
			Console.WriteLine( "\nMERGE FROM KEYRING:" );
			String from_keyring = SelectKeyring();
			
			int[] from_item_ids = Ring.ListItemIDs( from_keyring );
			Array.Sort( from_item_ids );
			Array.Reverse( from_item_ids );

			//int i = 0;
			foreach( int id in from_item_ids ) {
				try {
					//if( i > 5 ) {	break; } i++;

					ItemData from_info = Ring.GetItemInfo( from_keyring, id );
					Hashtable from_attr = Ring.GetItemAttributes( from_keyring, id );
					
					//Console.WriteLine( "{0}", from_info.Attributes["name"] );
					
					//DumpItemInfo( from_info, from_attr );
					
					//Console.Write( "\r" + id );
					
					ItemData[] matches = Ring.Find( from_info.Type, from_attr );
					
					int matches_i = 0;
					foreach( ItemData match_info in matches ) {
						if( match_info.Keyring != to_keyring ) continue;
						Hashtable match_attr = Ring.GetItemAttributes( to_keyring, match_info.ItemID );
						if( match_attr.Count != from_attr.Count ) continue;
						//Console.WriteLine( match_attr.GetHashCode() );
						matches_i++;
					}
					
					if( matches_i > 1 ) {
						Console.WriteLine( "-----------------------------------------------------" );

						Console.WriteLine( "GOT MULTIPLE ITEMS: {0}", from_info.Attributes["name"] );
						/*foreach( ItemData match_info in matches ) {
							if( match_info.Keyring != to_keyring ) continue;
							Hashtable match_attr = Ring.GetItemAttributes( to_keyring, match_info.ItemID );
							Console.WriteLine( match_attr.ToString() );
						}
						Console.WriteLine( "-" );
						Console.WriteLine( from_attr );
						Console.WriteLine( "!" );*/
					} else if( matches_i > 0 ) {
						//Console.WriteLine( " Found match, is updating needed?" );
						
						foreach( ItemData match_to_info in matches ) {
							if( match_to_info.Keyring != to_keyring ) continue;
							ItemData to_info = Ring.GetItemInfo( to_keyring, match_to_info.ItemID );
							Hashtable to_attr = Ring.GetItemAttributes( to_keyring, match_to_info.ItemID );
							if( to_attr.Count != from_attr.Count ) continue;
							
							if( to_info.Secret != from_info.Secret ) {
								Console.WriteLine( "-----------------------------------------------------" );
								
								Console.WriteLine( "Do you want to replace this:" );
								DumpItemInfo( to_info, to_attr );
								
								Console.WriteLine( "with THIS?:" );
								DumpItemInfo( from_info, from_attr );

								String answer = SelectYesNo( "Update? " );
								if( answer == "y" ) {
									Console.WriteLine( " Updating item" );
									Ring.SetItemInfo( to_keyring, to_info.ItemID, from_info.Type, from_info.Attributes["name"].ToString(), from_info.Secret );
									Ring.SetItemAttributes( to_keyring, to_info.ItemID, from_attr );
								} else {
									Console.WriteLine( " Skipping item" );
								}
							} else {
								//Console.WriteLine( "  Nope, secret is same" );
							}
						}
					} else {
						Console.WriteLine( "-----------------------------------------------------" );

						DumpItemInfo( from_info, from_attr );

						String answer = SelectYesNo( "Add? " );
						if( answer == "y" ) {
							Console.WriteLine( "Adding item" );
							Ring.CreateItem( to_keyring, from_info.Type, from_info.Attributes["name"].ToString(), from_attr, from_info.Secret, false );
						} else {
							Console.WriteLine( "Skipping item" );
						}
					}
				} catch( System.ArgumentException e ) {
					Console.WriteLine( "-----------------------------------------------------" );

					if( e.Message == "Unknown type: 4\nParameter name: type" ) {
						Console.WriteLine( "\nFaced a SSH passphrases (KeyID #{0} in '{1}' keyring), "+
						                  "which is not supported by the upstream library. You need to merge it manually.\n",
						                  id, from_keyring );
					} else {
						Console.WriteLine( "\nWorking with {0} ID# {1}\nGot thrown an error: {2}\n", from_keyring, id, e.Message );
					}
				}
			}
			
			Console.WriteLine( "-----------------------------------------------------" );
			
			String delete_keyring = SelectYesNo( "Delete keyring \"" + from_keyring + "\"? " );
			if( delete_keyring == "y" ) {
				Console.WriteLine( "Removing keyring" );
				Ring.DeleteKeyring( from_keyring );
			} else {
				Console.WriteLine( "Keeping keyring" );
			}
		}
		
		protected static void DumpItemInfo( ItemData from_info, Hashtable from_attr ) {
			Console.WriteLine( " ID: {0}", from_info.ItemID );
			foreach( string key in from_info.Attributes.Keys ) {
				Console.WriteLine( "      {0} = {1}", key, from_info.Attributes[key] );
			}
			foreach( string key in from_attr.Keys ) {
				Console.WriteLine( "      {0} = {1}", key, from_attr[key] );
			}
			Console.WriteLine( "      Secret = {0}", from_info.Secret );
		}
		
		protected static String SelectKeyring() {
			
			String[] keyrings = Ring.GetKeyrings();
			
			for( int i = 0; i < keyrings.Length; i++ ) {
				Console.WriteLine( "["+i+"] "+keyrings[i] );
			}
			
			String answer = "";
			while( answer == "" ) {
				Console.Write( ": " );
				
				try {
					Int16 num = Convert.ToInt16( Console.ReadLine() );
					
					if( num < 0 || num >= keyrings.Length ) {
						Console.WriteLine( "ERROR: Number not available" );
						continue;
					}
					
					answer = keyrings[num];
				} catch( System.FormatException e ) {
					Console.WriteLine( "ERROR: Input was not a number" );
					continue;
				}
			}
			
			
			while( true ) {
				
				KeyringInfo ki = Ring.GetKeyringInfo( answer );
				if( !ki.Locked ) {
					break;
				}
				
				Console.WriteLine( "The keyring is locked, please provide the password." );
				Console.Write( "(pwd): " );
				
				String pwd = "";
				ConsoleKeyInfo cki;
				while( true ) {
					cki = Console.ReadKey( true );
					
					if( cki.Key == ConsoleKey.Enter ) {
						break;
					}
					
					pwd = pwd + cki.KeyChar;
				}
				
				Console.WriteLine( "" );
				
				try {
					Ring.Unlock( answer, pwd );
				} catch( Gnome.Keyring.KeyringException e ) {
					Console.WriteLine( "Password incorrect. Try again." );
				}
			}
			
			return answer;
		}
		
		protected static String SelectYesNo( string prefix ) {
			
			String answer = "";
			
			while( answer == "" ) {
				Console.Write( prefix + "(y or n): " );
			
				String c = Console.ReadLine();
				
				if( c == "y" || c == "n" ) {
					answer = c;
				} else {
					Console.WriteLine( "ERROR: Answer must be y or n" );
				}
			}
			
			return answer;
		}
	}
}
