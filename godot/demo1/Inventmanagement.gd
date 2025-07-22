extends Control

var v2_TileSize: Vector2 = Vector2(32,32)
var v2_InventDimensions: Vector2 = Vector2(10,10)
var i_SelectItemIndex: int = 1000
var col_Invalidcol: Color = Color(1, 0.36, 0.36, 1)
var col_Validcol: Color = Color(1, 1, 1, 1)
var v2_CursorItemDragOff: Vector2 = Vector2(-16,-16)

var InventoryPanel: ColorRect
var InventoryGrids: Sprite2D
var Inventory: Area2D

var bl_IsItemSel: bool = false
var ctrl_SelItem: Control
var v2_ItemPrevPositon: Vector2
var bl_IsDragging: bool = false
var bl_IsSelItemInInventory: bool = false

var arr_OverlappingWith: Array
var dct_InventoryItems: Dictionary
var dct_InventoryItemSlots: Dictionary

# Called when the node enters the scene tree for the first time.
func _ready():
	InventoryPanel = $InventoryPanel
	InventoryGrids = $InventoryGrids
	Inventory = $InventoryGrids/Inventory
	
	Inventory.connect("area_entered", Callable(self, "item_inside_inventory"))
	Inventory.connect("area_exited", Callable(self, "item_outside_inventory"))
	
	#Add singal connection to item prefabs
	for ctrl_Item in get_tree().get_nodes_in_group("item"):
		add_signal_connection(ctrl_Item)
	#Add SortInventory Button Signal connection
	$btn_SortInventory.connect("button_up", Callable(self, "init_sort_inventory"))

func add_signal_connection(ctrl_Item: Control):
	#ctrl_Item.connect("gui_input", self, "cursor_in_item", [ctrl_Item])
	#有一个 Control 节点，并且你想在其上连接 gui_input 信号到 cursor_in_item 方法。
	ctrl_Item.connect("gui_input", Callable(self, "cursor_in_item").bind(ctrl_Item))
	#ctrl_Item.get_node("Sprite/Area2D").connect("area_entered", self, "overlapping_with_other_item",[ctrl_Item])
	#ctrl_Item.get_node("Sprite/Area2D").connect("area_exited", self, "not_overlapping_with_other_item",[ctrl_Item])
	ctrl_Item.get_node("Sprite2D/Area2D").connect("area_entered", Callable(self, "overlapping_with_other_item").bind(ctrl_Item))
	ctrl_Item.get_node("Sprite2D/Area2D").connect("area_exited", Callable(self, "not_overlapping_with_other_item").bind(ctrl_Item))

func remove_signal_connection(ctrl_Item: Control):
	if ctrl_Item.is_connected("gui_input", Callable(self, "cursor_in_item")):
		ctrl_Item.disconnect("gui_input", Callable(self, "cursor_in_item"))
	if ctrl_Item.get_node("Sprite2D/Area2D").is_connected("area_entered", Callable(self, "overlapping_with_other_item")):
		ctrl_Item.get_node("Sprite2D/Area2D").disconnect("area_entered", Callable(self, "overlapping_with_other_item"))
	if ctrl_Item.get_node("Sprite2D/Area2D").is_connected("area_exited", Callable(self, "not_overlapping_with_other_item")):
		ctrl_Item.get_node("Sprite2D/Area2D").disconnect("area_exited", Callable(self, "not_overlapping_with_other_item"))
		
func cursor_in_item(event: InputEvent, ctrl_Item: Control):
	if event.is_action_pressed("select_item"):
		bl_IsItemSel = true
		ctrl_SelItem = ctrl_Item
		ctrl_SelItem.get_node("Sprite2D").set_z_index(i_SelectItemIndex)
		v2_ItemPrevPositon = ctrl_SelItem.position
	
	if event is InputEventMouseButton:
		if bl_IsItemSel:
			bl_IsDragging = true
				
	if event.is_action_released("select_item"):
		ctrl_SelItem.get_node("Sprite2D").set_z_index(0)
		
		if arr_OverlappingWith.size() > 0:
			ctrl_SelItem.position = v2_ItemPrevPositon
			ctrl_SelItem.get_node("Sprite2D").modulate = col_Validcol
		else:
			# print("--",bl_IsSelItemInInventory," ",add_item_to_inventory(ctrl_SelItem))
			if bl_IsSelItemInInventory:				
				if !add_item_to_inventory(ctrl_SelItem):
					ctrl_SelItem.position = v2_ItemPrevPositon
					
		bl_IsItemSel = false
		bl_IsDragging = false
		ctrl_SelItem = null
	
func overlapping_with_other_item(area: Area2D, ctrl_Item: Control):
	if area.get_parent().get_parent() == ctrl_SelItem:
		return
		
	if area == Inventory:
		return
	
	arr_OverlappingWith.append(ctrl_Item)	
	
	if ctrl_SelItem:
		ctrl_SelItem.get_node("Sprite2D").modulate = col_Invalidcol
		
func not_overlapping_with_other_item(area: Area2D, ctrl_Item: Control):
	if area.get_parent().get_parent() == ctrl_SelItem:
		return
		
	if area == Inventory:
		return
		
	arr_OverlappingWith.erase(ctrl_Item)	
	
	if arr_OverlappingWith.size() == 0 and bl_IsItemSel:
		ctrl_SelItem.get_node("Sprite2D").modulate = col_Validcol
	
func _process(delta):
	if bl_IsDragging:
		ctrl_SelItem.position = (self.get_global_mouse_position() + v2_CursorItemDragOff).snapped(v2_TileSize)

func item_inside_inventory(body: Node2D):
	bl_IsSelItemInInventory = true

func item_outside_inventory(body: Node2D):
	bl_IsSelItemInInventory = false

func add_item_to_inventory(ctrl_Item: Control) -> bool:
	var v2_slotID: Vector2 = ctrl_Item.position / v2_TileSize
	var v2_ItemSlotSize: Vector2 = ctrl_Item.get_size() / v2_TileSize
	var v2_ItemMaxSlotID: Vector2 = v2_slotID + v2_ItemSlotSize - Vector2(1, 1)
	var v2_InventorySlotBounds: Vector2 = v2_InventDimensions - Vector2(1, 1)
	
	if v2_ItemMaxSlotID.x > v2_InventorySlotBounds.x:
		return false
	if v2_ItemMaxSlotID.y > v2_InventorySlotBounds.y:
		return false
	if dct_InventoryItems.has(ctrl_Item):
		remove_item_in_inventory_slot(ctrl_Item, dct_InventoryItems[ctrl_Item])
	for y_Ctr in range(v2_ItemSlotSize.y):
		for x_Ctr in range(v2_ItemSlotSize.x):
			dct_InventoryItemSlots[Vector2(v2_slotID.x + x_Ctr, v2_slotID.y + y_Ctr)] = ctrl_Item
	dct_InventoryItems[ctrl_Item] = v2_slotID
	
	return true

func remove_item_in_inventory_slot(ctrl_Item: Control, v2_ExistingslotID: Vector2):
	var v2_ItemSlotSize: Vector2 = ctrl_Item.get_size() / v2_TileSize
	
	for y_Ctr in range(v2_ItemSlotSize.y):
		for x_Ctr in range(v2_ItemSlotSize.x):
			if dct_InventoryItemSlots.has(Vector2(v2_ExistingslotID.x + x_Ctr, v2_ExistingslotID.y + y_Ctr)):
				dct_InventoryItemSlots.erase(Vector2(v2_ExistingslotID.x + x_Ctr, v2_ExistingslotID.y + y_Ctr))

func init_sort_inventory():
	if dct_InventoryItems.size() ==  0:
		return
	for ctrl_Item in dct_InventoryItems:
		remove_signal_connection(ctrl_Item)
		
	var arr_InventoryItem: Array
	for item in dct_InventoryItems:
		var v2_ItemSlotSize: Vector2 = item.get_size() / v2_TileSize
		arr_InventoryItem.append([item, v2_ItemSlotSize])
	
	arr_InventoryItem.sort_custom(height_priority_size_sorter)
	
	if sort_inventory(arr_InventoryItem):
		arr_InventoryItem.sort_custom(width_priority_size_sorter)
		sort_inventory((arr_InventoryItem))
	
	for ctrl_Item in dct_InventoryItems:
		add_signal_connection(ctrl_Item)
		
func height_priority_size_sorter(a,b):
	if a[1].y == b[1].y and a[1].x > b[1].x:
		return true
	if a[1].y > b[1].y:
		return true
	return false
	
func width_priority_size_sorter(a,b):
	if a[1].y == b[1].y and a[1].x > b[1].x:
		return true
	if a[1].x > b[1].x:
		return true
	return false
	
func sort_inventory(arr_InventoryItem: Array):
	dct_InventoryItemSlots.clear()
	
	var arr_InventoryBlankSlots: Array
	
	for col_ctr in v2_InventDimensions.x:
		for row_ctr in v2_InventDimensions.y:
			arr_InventoryBlankSlots.append(Vector2(col_ctr, row_ctr))
	# print(arr_InventoryBlankSlots)		
	var ctrl_PrevItem: Control = arr_InventoryItem[0][0]
	var i_ItemCtr: int = 0
	var bl_IsSlotAvail: bool
	
	for item in arr_InventoryItem:
		if i_ItemCtr > 0:
			ctrl_PrevItem = arr_InventoryItem[i_ItemCtr-1][0]
		var ctrl_Item: Control = item[0]
		var v2_ItemSlotSize: Vector2 = item[1]
		var arr_AssginedSlots: Array
		
		for v2_BlankSlot in arr_InventoryBlankSlots:
			bl_IsSlotAvail = true
			var v2_UpperLeftSoltID: Vector2
			
			var arr_ItemDimensionIDs: Array
			for WidthCtr in v2_ItemSlotSize.x:
				for LenCtr in v2_ItemSlotSize.y:
					if WidthCtr == 0 and LenCtr == 0:
						v2_UpperLeftSoltID = v2_BlankSlot
					arr_ItemDimensionIDs.append(Vector2(WidthCtr, LenCtr))
			# print("dimid",arr_ItemDimensionIDs)		
			# print("ul",v2_UpperLeftSoltID)
			for v2_DimensionID in arr_ItemDimensionIDs:
				var v2_SlotID: Vector2 = v2_BlankSlot + v2_DimensionID
				
				if v2_SlotID.y >= v2_InventDimensions.y:
					bl_IsSlotAvail = false
					arr_AssginedSlots.clear()
					break
				if v2_SlotID.x >= v2_InventDimensions.x:
					bl_IsSlotAvail = false
					arr_AssginedSlots.clear()
					break
					
				if arr_InventoryBlankSlots.find(v2_SlotID) != -1:
					arr_AssginedSlots.append(v2_SlotID)
				else:
					bl_IsSlotAvail = false
					arr_AssginedSlots.clear()
					break
					
			if bl_IsSlotAvail:
				for v2_AssignedSlotID in arr_AssginedSlots:
					arr_InventoryBlankSlots.erase(v2_AssignedSlotID)
					dct_InventoryItemSlots[v2_AssignedSlotID] = ctrl_Item
				arr_AssginedSlots.clear()
				ctrl_Item.position = v2_UpperLeftSoltID * v2_TileSize
				break
			
		i_ItemCtr += 1
		if i_ItemCtr == arr_InventoryItem.size():
			if !bl_IsSlotAvail:
				return false
			return true
