# Faction-Flavor-Expansion
 A mod for the game "Rimworld" that aims to add to the faction system.
 
 Faction History:
	Generates each faction's past events and from that decides if the faction is aggressive or peaceful in a value called disposition.
 
 Faction War:
 
	-Each faction has a resource pool that is generated from its settlements count, their disposition and their tech level.
	
	-The resource pool of a faction is depleted when a pawn belonging to it dies, a settlement is destroyed or through war events.
	
	-Every faction can go to war with another. The war ends when either faction in the war hits 0 resources with three outcomes.
		1. White peace, the war just ends and both get a bonus to revitalize their resource pool.
		2. The losing faction is broken down to several smaller ones.
		3. Total defeat, the winner absorbs the loser gaining all their settlements.
	
	- War events:
		1. Settlement raided and destroyed.
		2. Settlement captured.
		3. Faction found a valuable artifact cache.
		4. Farms burned.
		5. Supply depot established accelerating the faction resource regain.
		6. caravan ambushed.
		7. Minor outpost raided.
		8. A raid on a settlement failed.
		9. Settlement nuked, every adjacent settlement might experience toxic fallout.
		10. War factories sabotaged.
		
	- Faction Expansion: Faction can use resources to build new settlements.
	
	
		
Vassals and Tributaries:

	- Vassal: The player can force faction with low resource pool to become a vassal, the player can then demand resources every few days and will receive a payment at the first day of the year.
	
	Vassal Investments:
	After subjugating a faction you can invest in different categories. Each upgrade will guarantee a return payment at the start of the month. A vassal turning hostile will stop the payments until you get them back under control.

	- Armory {Poor=> Normal=> Good=> Excellent}
	- Weaponry {Poor=> Normal=> Good=> Excellent}
	- Mining {Wood, Stone => Wood, Stone, Steel => Wood, Stone, Steel, Jade, Gold => Wood, Stone, Steel, Jade, Gold, Plasteel, Uranium}
	- Medicine {Herbal => Regular Medicine=> Glitterworld Medicine}
	- Druglabs {Beer, Smokeleaf => Beer, Smokeleaf, Penoxycyline, Psychite Tea => Last items, Flake, Yayo, Wake-up, Go-juice => last items,Luciferium}
	- Prostheticslabs {Prosthetic leg, Prosthetic arm => Bionic leg, Bionic arm=> Bionic leg, Bionic arm, Bionic eye, Bionic ear => Bionic leg, Bionic arm, Bionic eye, Bionic ear, Bionic spine, Bionic stomach}
	- Food {Stack of plant matter=> 2 stacks, Stack of meat=> 5 stacks}
	- Components { Components 30 => Components 60 => Components 60, Advanced component 10}
	- Trade {Silver, More silver, even more silver}
	- Relations {0, +5, +10}

	
	- Tributaries: A tributary is like a vassal that will pay monthly instead of yearly but won't agree to hand over rare resource for free.
	
Incidents:

	- Joint Raid: An ally faction requests you help them in their raid of an enemy settlement, they will give a bonus reward of silver if at least one of their fighter is alive after the battle.
	
	- Settlement Defense: An ally faction requests you help them defense against an upcoming raid of one of their settlements, if you are successful they might decide it is the perfect time to counterstrike and a joint raid offer will come.
	
	- Outpost Defense: An ally faction warns you that a hostile faction is trying to take over a strategic outpost in order to turn it to a base of operation that will launch a powerful raid against you, help them repeal the take over and help your self in a process.
	
	- Faction Advancement: An ally faction discovered a new technology that you don't already possess and they are willing to sell it to you.
	
	- Dispute: Two settlement that belong to an ally faction have a problem they hope you can help them settle, several outcomes.
	
	- Aid: An ally faction sent you supplies when they heard about your troubles.
	
	- Gift: An non hostile faction has sent you a gift.
	
	- Mercenary Battle: a faction that is losing its war is hiring mercenaries for an important battle.
	
	- Skirmish: Your caravan was too close to an settlement whose faction is at war and ran straight into a small scale battle between the warring factions.
	
	- Bombardment: An hostile faction with a settlement a least as close as 20 tile will threaten carpet bombing of the colony unless you pay for protection.