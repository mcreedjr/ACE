USE `ace_world`;

ALTER TABLE `cook_book` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `target_W_C_I_D`;

ALTER TABLE `encounter` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `cell_Y`;

ALTER TABLE `event` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `state`;

ALTER TABLE `house_portal` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `angles_Z`;

ALTER TABLE `landblock_instance` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `is_Link_Child`;

ALTER TABLE `landblock_instance_link` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `child_GUID`;

ALTER TABLE `points_of_interest` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `weenie_Class_Id`;

ALTER TABLE `quest` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `message`;

ALTER TABLE `recipe` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `data_Id`;

ALTER TABLE `spell` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `dot_Duration`;

ALTER TABLE `treasure_death` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `mundane_Item_Type_Selection_Chances`;

ALTER TABLE `treasure_wielded` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `unknown_12`;

ALTER TABLE `weenie` 
ADD COLUMN `last_Modified` DATETIME NOT NULL DEFAULT NOW() ON UPDATE NOW() AFTER `type`;