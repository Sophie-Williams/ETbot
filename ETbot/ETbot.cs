using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Resources;
using Resources.Packet;
using Resources.Utilities;
using Resources.Packet.Part;
using System.Threading;
using System.Diagnostics;

namespace ETbot {
    static class ETbot {
        static TcpClient connection;
        static BinaryReader reader;
        static BinaryWriter writer;
        static long personalGuid;
        static Dictionary<long, EntityUpdate> players;
        static Stopwatch stopWatch = new Stopwatch();
        static long maloxGuid;

        public static void Connect(string hostname, int port) {
            players = new Dictionary<long, EntityUpdate>();
            connection = new TcpClient(hostname, port);
            var thatStream = connection.GetStream();
            reader = new BinaryReader(thatStream);
            writer = new BinaryWriter(thatStream);

            var zumServerhallosagen = new ProtocolVersion {
                version = 3,
            };
            zumServerhallosagen.Write(writer);

            

            while (true) {
                var packetid = reader.ReadInt32();
                ProcessPacket(packetid);
            }
        }

        static void ProcessPacket(int packetid) {
            switch (packetid) {
                case 0:
                    #region entityUpdate
                    var entityUpdate = new EntityUpdate(reader);
                    if (players.ContainsKey(entityUpdate.guid)) {
                        entityUpdate.Merge(players[entityUpdate.guid]);
                    }
                    else {
                        players.Add(entityUpdate.guid, entityUpdate);
                    }
                    if (players[entityUpdate.guid].name == "malox") {
                        maloxGuid = entityUpdate.guid;
                        var opplayer = new EntityUpdate();
                        //var x = players[entityUpdate.guid].position.x - players[personalGuid].position.x;
                        //var y = players[entityUpdate.guid].position.y - players[personalGuid].position.y;
                        //double distance = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
                        //if (distance > 65536 * 40) {
                        //    var follow = new EntityUpdate {
                        //        position = players[entityUpdate.guid].position,
                        //        guid = personalGuid
                        //    };
                        //    follow.Write(writer);
                        //}
                        if (entityUpdate.modeTimer < 25) {
                            var shoot = new Shoot() {
                                attacker = personalGuid,
                                chunkX = (int)players[personalGuid].position.x / 0x1000000,
                                chunkY = (int)players[personalGuid].position.y / 0x1000000,
                                position = players[personalGuid].position,
                                particles = 100,
                                mana = 100,
                                scale = 2f,
                                projectile = 0,
                            };
                            shoot.position.x = players[maloxGuid].position.x + (long)(players[maloxGuid].rayHit.x * 0x10000);
                            shoot.position.y = players[maloxGuid].position.y + (long)(players[maloxGuid].rayHit.y * 0x10000);
                            shoot.position.z = players[maloxGuid].position.z + (long)((players[maloxGuid].rayHit.z + 15)* 0x10000);

                            shoot.velocity.z = -40f;

                            //shoot.velocity.x = (float)players[maloxGuid].position.x / 0x10000f + players[maloxGuid].rayHit.x - (float)players[personalGuid].position.x / 0x10000f;
                            //shoot.velocity.y = (float)players[maloxGuid].position.y / 0x10000f + players[maloxGuid].rayHit.y - (float)players[personalGuid].position.y / 0x10000f;


                            //shoot.velocity.z = (float)players[maloxGuid].position.z / 0x10000f + players[maloxGuid].rayHit.z - (float)players[personalGuid].position.z / 0x10000f;
                            int range = 20;
                            shoot.position.x -= (range - 1) / 2 * 0x10000;
                            shoot.position.y -= (range - 1) / 2 * 0x10000;
                            for (int i = 0; i < range; i++) {
                                for (int j = 0; j < range; j++) {
                                    shoot.Write(writer);
                                    shoot.position.x += 0x10000;
                                }
                                shoot.position.x -= range * 0x10000;
                                shoot.position.y += 0x10000;
                            }
                        }
                    }
                    
                    break;
                #endregion
                case 2:
                    #region complete
                    //empty
                    break;
                #endregion
                case 4:
                    #region serverupdate
                    var serverUpdate = new ServerUpdate(reader);
                    foreach (var hit in serverUpdate.hits) {
                        if (hit.target == personalGuid) {
                            stopWatch.Restart();
                            players[personalGuid].HP -= hit.damage;
                            var life = new EntityUpdate() {
                                HP = players[personalGuid].HP,
                            };


                            life.Write(writer);
                            AntiTimeOut(new EntityUpdate());
                            if (players[personalGuid].HP <= 0) {
                                life.HP = 1623f;
                                life.Write(writer);
                            }
                        }
                        //if (hit.target == maloxGuid) {
                        //var heal = new Hit {
                        //    attacker = personalGuid,
                        //    target = maloxGuid,
                        //    damage = -1 * hit.damage,
                        //};
                        //heal.Write(writer);
                        //}

                    }
                    //var size = reader.ReadInt32();
                    //var compressed = reader.ReadBytes(size);
                    break;
                #endregion
                case 5:
                    #region time
                    var time = new Time(reader);
                    break;
                #endregion
                case 10:
                    #region chat
                    var chatMessage = new ChatMessage(reader, true);
                    if (chatMessage.sender == 0) {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    }
                    else {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(players[chatMessage.sender].name + ": ");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine(chatMessage.message);
                    if (chatMessage.message == "!set") {
                        
                            var port = new EntityUpdate {
                            position = players[chatMessage.sender].position,
                            guid = personalGuid
                        };
                        port.Write(writer);
                        players[personalGuid].position = port.position;

                        var items = new List<Item>();

                        var rng = new Random();
                        for (byte i = 3; i <= 9; i++) {
                            items.Add(new Item() {
                                type = i,
                                modifier = rng.Next(0x7FFFFFFF),
                                rarity = 4,
                                level = (short)players[chatMessage.sender].level 
                            });
                        }
                        items[5].material = (byte)rng.Next(11,12);
                        items[6].material = (byte)rng.Next(11,12);
                        items.Add(items[6]);
                        byte armorMaterial;
                        switch (players[chatMessage.sender].entityClass) {
                            case 0: //warrior
                                //items[0].subtype = 0;
                                items[0].material = 1;
                                for (int i = 0; i < 6; i++) {
                                    items.Add(new Item() {
                                        type = 3,
                                        material = 1,
                                        modifier = rng.Next(0x7FFFFFFF),
                                        rarity = 4,
                                        level = (short)players[chatMessage.sender].level
                                    });
                                }
                                items[8].subtype = 1;
                                items[9].subtype = 2;
                                items[10].subtype = 13;
                                items[11].subtype = 15;
                                items[12].subtype = 16;
                                items[13].subtype = 17;
                                    
                                armorMaterial = 1;
                                break;

                            case 1: //mage
                                items[0].subtype = 10;
                                items[0].material = 2;
                                for (int i = 0; i < 3; i++) {
                                    items.Add(new Item() {
                                        type = 3,
                                        material = (byte)rng.Next(11,12),
                                        modifier = rng.Next(0x7FFFFFFF),
                                        rarity = 4,
                                        level = (short)players[chatMessage.sender].level
                                    });
                                }

                                items[8].subtype = 11;
                                items[8].material = 2;
                                items[9].subtype = 12;
                                items[10].subtype = 12;

                                armorMaterial = 25;
                                break;


                            case 2: //ranger
                                items[0].subtype = 6;
                                items[0].material = 2;
                                for (int i = 0; i < 2; i++) {
                                    items.Add(new Item() {
                                        type = 3,
                                        material = 2,
                                        modifier = rng.Next(0x7FFFFFFF),
                                        rarity = 4,
                                        level = (short)players[chatMessage.sender].level
                                    });
                                }
                                items[8].subtype = 7;
                                items[9].subtype = 8;

                                armorMaterial = 26;
                                break;

                            case 3: //rogue
                                items[0].subtype = 3;
                                items[0].material = 1;
                                for (int i = 0; i < 2; i++) {
                                    items.Add(new Item() {
                                        type = 3,
                                        material = 1,
                                        modifier = rng.Next(0x7FFFFFFF),
                                        rarity = 4,
                                        level = (short)players[chatMessage.sender].level
                                    });
                                }
                                items[8].subtype = 4;
                                items[9].subtype = 5;

                                armorMaterial = 27;
                                break;

                            default:
                                goto case 0;
                        }
                        for (int i = 1; i <= 4; i++) {
                            items[i].material = armorMaterial;
                        }

                        foreach (var that in items) {
                            var drop = new EntityAction {
                                type = (int)Database.ActionType.drop,
                                item = that
                            };
                            drop.Write(writer);
                        }

                        //item = new Item {
                        //    type = 3, // 3 = weapon
                        //    rarity = 4, // 4 = legendary
                        //    level = 647,
                        //    material = 1
                        //}


                    }
                    
                    break;
                #endregion
                case 15:
                    #region mapseed
                    var mapSeed = new MapSeed(reader);
                    break;
                #endregion
                case 16:
                    #region join
                    var join = new Join(reader);
                    personalGuid = join.guid;

                    var playerstats = new EntityUpdate() {
                        position = new LongVector(),
                        rotation = new FloatVector(),
                        velocity = new FloatVector(),
                        acceleration = new FloatVector(),
                        extraVel = new FloatVector(),
                        viewportPitch = 0,
                        physicsFlags = 0b00000000_00000000_00000000_00010001,//17
                        hostility = 0,
                        entityType = 0,
                        mode = 0,
                        modeTimer = 0,
                        combo = 0,
                        lastHitTime = 0,
                        appearance = new Appearance() {
                            character_size = new FloatVector() {
                                x = 0.9600000381f,
                                y = 0.9600000381f,
                                z = 2.160000086f
                            },
                            head_model = 1236,
                            hair_model = 1280,
                            hand_model = 430,
                            foot_model = 432,
                            body_model = 1,
                            tail_model = -1,
                            shoulder2_model = -1,
                            wings_model = -1,
                            head_size = 1.00999999f,
                            body_size = 1f,
                            hand_size = 1f,
                            foot_size = 0.9800000191f,
                            shoulder2_size = 1f,
                            weapon_size = 0.9499999881f,
                            tail_size = 0.8000000119f,
                            shoulder_size = 1f,
                            wings_size = 1f,
                            body_offset = new FloatVector() {
                                z = -5f
                            },
                            head_offset = new FloatVector() {
                                y = 0.5f,
                                z = 5f
                            },
                            hand_offset = new FloatVector() {
                                x = 6f,
                            },
                            foot_offset = new FloatVector() {
                                x = 3f,
                                y = 1f,
                                z = -10.5f
                            },
                            back_offset = new FloatVector() {
                                y = -8f,
                                z = 2f
                            },
                        },
                        entityFlags = 0b00000000_00000000_00000000_00100000,//64
                        roll = 0,
                        stun = 0,
                        slow = 0,
                        ice = 0,
                        wind = 0,
                        showPatchTime = 0,
                        entityClass = 2,
                        specialization = 0,
                        charge = 0,
                        unused24 = new FloatVector(),
                        unused25 = new FloatVector(),
                        rayHit = new FloatVector(),
                        HP = 1623f,
                        MP = 0,
                        block = 1,
                        multipliers = new Multipliers() {
                            HP = 100,
                            attackSpeed = 1,
                            damage = 1,
                            armor = 1,
                            resi = 1,
                        },
                        unused31 = 0,
                        unused32 = 0,
                        level = 500,
                        XP = 0,
                        parentOwner = 0,
                        unused36 = 0,
                        powerBase = 0,
                        unused38 = 0,
                        unused39 = new IntVector(),
                        spawnPos = new LongVector(),
                        unused41 = new IntVector(),
                        unused42 = 0,
                        consumable = new Item(),
                        equipment = new Item[13],
                        name = "ET_bot",
                        skillDistribution = new SkillDistribution() {
                            ability1 = 5,
                            ability2 = 5,
                            ability3 = 5,
                        },
                        manaCubes = 0,
                    };
                    for (int i = 0; i < 13; i++) {
                        playerstats.equipment[i] = new Item();
                    }
                    playerstats.Write(writer);
                    stopWatch.Start();
                    players.Add(personalGuid, playerstats);

                    //AntiTimeOut(new EntityUpdate());
                    
                    break;
                #endregion
                case 17: //serving sending the right version if yours is wrong
                    #region version
                    var version = new ProtocolVersion(reader);
                    break;
                #endregion
                case 18:
                    #region server full
                    //empty
                    break;
                #endregion
                default:
                    Console.WriteLine(string.Format("unknown packet id: {0}", packetid));
                    break;

            }
        }

        static void AntiTimeOut(EntityUpdate packet) {
            packet.lastHitTime = (int)stopWatch.ElapsedMilliseconds;
            packet.Write(writer);
            //Task.Delay(100).ContinueWith(t => AntiTimeOut(packet));
        }
    }   
}
