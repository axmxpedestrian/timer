import os


class Card:
    def __init__(self, name, hp, attack, defense, element, rarity):
        self.name = name
        self.hp = hp
        self.attack = attack
        self.defense = defense
        self.element = element
        self.rarity = rarity
        self.score = hp + 4 * attack + 4 * defense

    def __str__(self):
        return (f"名称: {self.name}\t"
                f"血量: {self.hp}\t"
                f"攻击力: {self.attack}\t"
                f"防御力: {self.defense}\t"
                f"属性: {self.element}\t"
                f"稀有度: {self.rarity}\t"
                f"赋分: {self.score}")


class CardManager:
    def __init__(self):
        self.cards = []
        self.element_relations = {
            '火': {'克': ['木','冰','兽'], '被克': ['水','岩']},
            '水': {'克': ['火'], '被克': ['电']},
            '木': {'克': ['电','土'], '被克': ['火']},
            '电': {'克': ['水'], '被克': ['木','光明']},
            '冰': {'克': ['龙'], '被克': ['火','神秘']},
            '土': {'克': ['火'], '被克': ['水','木']},
            '岩': {'克': ['电','虫'], '被克': ['水','木','神秘']},
            '虫': {'克': ['木'], '被克': ['龙']},
            '兽': {'克': ['水','虫'], '被克': ['火','电']},
            '龙': {'克': ['龙'], '被克': ['龙','神秘']},
            '神秘': {'克': ['冰'], '被克': ['暗']},
            '光明': {'克': ['暗'], '被克': ['神秘']},
            '暗': {'克': ['神秘'], '被克': ['光明']},
        }

    def find_card_by_name(self, name):
        """通过名称查找卡牌，返回索引和卡牌对象"""
        for i, card in enumerate(self.cards):
            if card.name == name:
                return i, card
        return -1, None

    def create_card(self):
        print("\n创建新卡牌")
        name = input("输入卡牌名称: ")
        # 检查是否已存在同名卡牌
        index, existing_card = self.find_card_by_name(name)
        if existing_card:
            print(f"已存在同名卡牌 {name}!")
            choice = input("是否要覆盖? (y/n): ").lower()
            if choice != 'y':
                return

        hp = int(input("输入血量: "))
        attack = int(input("输入攻击力: "))
        defense = int(input("输入防御力: "))
        element = input("输入属性(火/水/木/电/冰/土/岩/虫/兽/龙/神秘/光明/暗): ")
        rarity = input("输入稀有度: ")

        new_card = Card(name, hp, attack, defense, element, rarity)

        if existing_card:
            self.cards[index] = new_card
            print(f"卡牌 {name} 已更新!")
        else:
            self.cards.append(new_card)
            print(f"卡牌 {name} 创建成功!")

    def modify_card(self):
        print("\n修改卡牌")
        name = input("输入要修改的卡牌名称: ")
        index, card = self.find_card_by_name(name)

        if card is None:
            print(f"未找到卡牌 {name}!")
            return

        print("\n当前卡牌信息:")
        print(card)
        print("\n输入新信息(留空则保持原值):")

        new_hp = input(f"血量 [{card.hp}]: ")
        new_attack = input(f"攻击力 [{card.attack}]: ")
        new_defense = input(f"防御力 [{card.defense}]: ")
        new_element = input(f"属性 [{card.element}]: ")
        new_rarity = input(f"稀有度 [{card.rarity}]: ")

        # 更新卡牌属性
        card.hp = int(new_hp) if new_hp else card.hp
        card.attack = int(new_attack) if new_attack else card.attack
        card.defense = int(new_defense) if new_defense else card.defense
        card.element = new_element if new_element else card.element
        card.rarity = new_rarity if new_rarity else card.rarity
        card.score = card.hp + 4 * card.attack + 4 * card.defense

        print(f"卡牌 {name} 修改成功!")

    def import_cards(self):
        file_path = input("输入要导入的txt文件路径: ")
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                imported_count = 0
                updated_count = 0
                for line in file:
                    if line.strip():
                        data = line.strip().split(',')
                        if len(data) == 6:
                            name, hp, attack, defense, element, rarity = data
                            hp = int(hp)
                            attack = int(attack)
                            defense = int(defense)

                            # 检查是否已存在同名卡牌
                            index, existing_card = self.find_card_by_name(name)
                            new_card = Card(name, hp, attack, defense, element, rarity)

                            if existing_card:
                                self.cards[index] = new_card
                                updated_count += 1
                            else:
                                self.cards.append(new_card)
                                imported_count += 1

                print(f"导入完成! 新增卡牌: {imported_count}, 更新卡牌: {updated_count}")
        except FileNotFoundError:
            print("文件未找到!")
        except Exception as e:
            print(f"导入失败: {e}")

    def delete_card(self):
        name = input("输入要删除的卡牌名称: ")
        index, card = self.find_card_by_name(name)
        if card:
            del self.cards[index]
            print(f"卡牌 {name} 已删除!")
        else:
            print(f"未找到卡牌 {name}!")

    def export_cards(self):
        file_path = input("输入要导出的txt文件路径: ")
        mode = 'w'  # 默认覆盖模式

        # 检查文件是否存在
        if os.path.exists(file_path):
            choice = input("文件已存在，是否覆盖? (y-覆盖/n-追加/u-更新同名卡牌): ").lower()
            if choice == 'n':
                mode = 'a'
            elif choice == 'u':
                # 更新模式：读取现有文件，更新同名卡牌，保留不同名卡牌
                try:
                    existing_cards = []
                    with open(file_path, 'r', encoding='utf-8') as file:
                        for line in file:
                            if line.strip():
                                data = line.strip().split(',')
                                if len(data) == 6:
                                    existing_cards.append(data)

                    # 创建名称到卡牌的映射
                    card_dict = {card.name: card for card in self.cards}

                    # 更新现有卡牌
                    updated = 0
                    for i, card_data in enumerate(existing_cards):
                        name = card_data[0]
                        if name in card_dict:
                            card = card_dict[name]
                            existing_cards[i] = [
                                card.name,
                                str(card.hp),
                                str(card.attack),
                                str(card.defense),
                                card.element,
                                card.rarity
                            ]
                            updated += 1

                    # 添加新卡牌
                    added = 0
                    existing_names = {card[0] for card in existing_cards}
                    for card in self.cards:
                        if card.name not in existing_names:
                            existing_cards.append([
                                card.name,
                                str(card.hp),
                                str(card.attack),
                                str(card.defense),
                                card.element,
                                card.rarity
                            ])
                            added += 1

                    # 写回文件
                    with open(file_path, 'w', encoding='utf-8') as file:
                        for card_data in existing_cards:
                            file.write(','.join(card_data) + '\n')

                    print(f"导出完成! 更新卡牌: {updated}, 新增卡牌: {added}")
                    return
                except Exception as e:
                    print(f"更新导出失败: {e}")
                    return

        # 普通导出模式（覆盖或追加）
        try:
            with open(file_path, mode, encoding='utf-8') as file:
                for card in self.cards:
                    file.write(
                        f"{card.name},{card.hp},{card.attack},{card.defense},{card.element},{card.rarity}\n"
                    )
            print(f"卡牌已导出到 {file_path} (模式: {'覆盖' if mode == 'w' else '追加'})!")
        except Exception as e:
            print(f"导出失败: {e}")

    def search_card(self):
        name = input("输入要查找的卡牌名称: ")
        index, card = self.find_card_by_name(name)
        if card:
            print("\n卡牌详细信息:")
            print(card)
        else:
            print(f"未找到卡牌 {name}!")

    def list_all_cards(self):
        if not self.cards:
            print("当前没有卡牌!")
            return

        print("\n所有卡牌列表:")
        for i, card in enumerate(self.cards, 1):
            print(f"\n卡牌 #{i}")
            print(card)

    def show_element_table(self):
        print("\n属性克制表:")
        for element, relations in self.element_relations.items():
            print(f"{element}属性: 克制 {relations['克'] if relations['克'] else '无'}, "
                  f"被 {relations['被克'] if relations['被克'] else '无'} 克制")

    def calculate_attack(self, attacker, defender):
        # 检查属性克制关系
        attacker_element = attacker.element
        defender_element = defender.element

        # 获取攻击者的克制关系
        relations = self.element_relations.get(attacker_element, {})

        # 攻击者克制防御者
        if defender_element in relations.get('克',[]):
            return attacker.attack * 1.5
        # 攻击者被防御者克制
        elif defender_element in relations.get('被克',[]):
            return attacker.attack * 0.5
        # 无克制关系
        else:
            return attacker.attack

    def simulate_battle(self):
        if len(self.cards) < 2:
            print("至少需要两张卡牌才能对战!")
            return

        print("\n可用的卡牌:")
        for i, card in enumerate(self.cards, 1):
            print(f"{i}. {card.name}")

        try:
            index1 = int(input("选择第一张卡牌(输入序号): ")) - 1
            index2 = int(input("选择第二张卡牌(输入序号): ")) - 1

            if index1 < 0 or index1 >= len(self.cards) or index2 < 0 or index2 >= len(self.cards):
                print("无效的卡牌序号!")
                return

            card1 = self.cards[index1]
            card2 = self.cards[index2]

            print(f"\n对战开始: {card1.name} vs {card2.name}")

            # 创建副本以避免修改原始卡牌数据
            c1 = Card(card1.name, card1.hp, card1.attack, card1.defense, card1.element, card1.rarity)
            c2 = Card(card2.name, card2.hp, card2.attack, card2.defense, card2.element, card2.rarity)

            round_num = 1
            while c1.hp > 0 and c2.hp > 0 and round_num <= 20:  # 最多20回合防止无限循环
                print(f"\n回合 {round_num}:")

                # 计算实际攻击力（考虑属性克制）
                attack1 = self.calculate_attack(c1, c2)
                attack2 = self.calculate_attack(c2, c1)

                # 计算伤害
                damage1 = max(1, attack1 - c2.defense) if attack1 > c2.defense else 1
                damage2 = max(1, attack2 - c1.defense) if attack2 > c1.defense else 1

                # 应用伤害
                c2.hp -= damage1
                c1.hp -= damage2

                # 确保血量不低于0
                c1.hp = max(0, c1.hp)
                c2.hp = max(0, c2.hp)

                print(f"{c1.name} 攻击 {c2.name}, 造成 {damage1} 点伤害")
                print(f"{c2.name} 攻击 {c1.name}, 造成 {damage2} 点伤害")
                print(f"当前状态: {c1.name} HP={c1.hp}, {c2.name} HP={c2.hp}")

                round_num += 1

            # 判断胜负
            if round_num >= 20:
                if (c1.hp <= 0 and c2.hp <= 0) or c1.hp == c2.hp:
                    print("\n对战结果: 平局!")
                elif c1.hp > c2.hp:
                    print(f"\n对战结果: {c1.name} 获胜!")
                else:
                    print(f"\n对战结果: {c2.name} 获胜!")
            elif c1.hp <= 0 and c2.hp <= 0:
                print("\n对战结果: 平局!")
            elif c1.hp <= 0:
                print(f"\n对战结果: {c2.name} 获胜!")
            else:
                print(f"\n对战结果: {c1.name} 获胜!")

        except ValueError:
            print("请输入有效的数字序号!")



def main():
    manager = CardManager()

    while True:
        print("\n卡牌管理系统")
        print("1. 创建卡牌")
        print("2. 修改卡牌")
        print("3. 导入卡牌")
        print("4. 删除卡牌")
        print("5. 导出卡牌")
        print("6. 检索卡牌")
        print("7. 列出所有卡牌")
        print("8. 查看属性克制表")
        print("9. 模拟对战")
        print("0. 退出")

        choice = input("请选择操作: ")

        if choice == '1':
            manager.create_card()
        elif choice == '2':
            manager.modify_card()
        elif choice == '3':
            manager.import_cards()
        elif choice == '4':
            manager.delete_card()
        elif choice == '5':
            manager.export_cards()
        elif choice == '6':
            manager.search_card()
        elif choice == '7':
            manager.list_all_cards()
        elif choice == '8':
            manager.show_element_table()
        elif choice == '9':
            manager.simulate_battle()
        elif choice == '10':
            manager.battle_royale()
        elif choice == '0':
            print("感谢使用卡牌管理系统!")
            break
        else:
            print("无效的选择，请重新输入!")


if __name__ == "__main__":
    main()