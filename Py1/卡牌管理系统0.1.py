import os
import json


class Card:
    def __init__(self, name, hp, attack, defense, element, rarity):
        self.name = name
        self.hp = hp
        self.attack = attack
        self.defense = defense
        self.element = element
        self.rarity = rarity
        self.score = hp + 4 * attack + 3 * defense

    def __str__(self):
        return (f"名称: {self.name}\n"
                f"血量: {self.hp}\n"
                f"攻击力: {self.attack}\n"
                f"防御力: {self.defense}\n"
                f"属性: {self.element}\n"
                f"稀有度: {self.rarity}\n"
                f"赋分: {self.score}")


class CardManager:
    def __init__(self):
        self.cards = []
        self.element_relations = {
            '火': {'克': '木', '被克': '水'},
            '水': {'克': '火', '被克': '电'},
            '电': {'克': '水', '被克': '木'},
            '木': {'克': '电', '被克': '火'},
            '土': {'克': [], '被克': []}  # 土属性没有克制关系
        }

    def create_card(self):
        print("\n创建新卡牌")
        name = input("输入卡牌名称: ")
        hp = int(input("输入血量: "))
        attack = int(input("输入攻击力: "))
        defense = int(input("输入防御力: "))
        element = input("输入属性(火/水/电/木/土): ")
        rarity = input("输入稀有度: ")

        new_card = Card(name, hp, attack, defense, element, rarity)
        self.cards.append(new_card)
        print(f"卡牌 {name} 创建成功!")

    def import_cards(self):
        file_path = input("输入要导入的txt文件路径: ")
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                for line in file:
                    if line.strip():
                        data = line.strip().split(',')
                        if len(data) == 6:
                            name, hp, attack, defense, element, rarity = data
                            new_card = Card(
                                name,
                                int(hp),
                                int(attack),
                                int(defense),
                                element,
                                rarity
                            )
                            self.cards.append(new_card)
            print(f"从 {file_path} 导入卡牌成功!")
        except FileNotFoundError:
            print("文件未找到!")
        except Exception as e:
            print(f"导入失败: {e}")

    def delete_card(self):
        name = input("输入要删除的卡牌名称: ")
        for i, card in enumerate(self.cards):
            if card.name == name:
                del self.cards[i]
                print(f"卡牌 {name} 已删除!")
                return
        print(f"未找到卡牌 {name}!")

    def export_cards(self):
        file_path = input("输入要导出的txt文件路径: ")
        try:
            with open(file_path, 'a', encoding='utf-8') as file:
                for card in self.cards:
                    file.write(
                        f"{card.name},{card.hp},{card.attack},{card.defense},{card.element},{card.rarity}\n"
                    )
            print(f"卡牌已导出到 {file_path}!")
        except Exception as e:
            print(f"导出失败: {e}")

    def search_card(self):
        name = input("输入要查找的卡牌名称: ")
        for card in self.cards:
            if card.name == name:
                print("\n卡牌详细信息:")
                print(card)
                return
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
        if relations.get('克') == defender_element:
            return attacker.attack * 1.5
        # 攻击者被防御者克制
        elif relations.get('被克') == defender_element:
            return attacker.attack * 0.5
        # 无克制关系
        else:
            return attacker.attack

    def simulate_battle(self):
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
            while c1.hp > 0 and c2.hp > 0:
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
            if c1.hp <= 0 and c2.hp <= 0:
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
        print("2. 导入卡牌")
        print("3. 删除卡牌")
        print("4. 导出卡牌")
        print("5. 检索卡牌")
        print("6. 列出所有卡牌")
        print("7. 查看属性克制表")
        print("8. 模拟对战")
        print("0. 退出")

        choice = input("请选择操作: ")

        if choice == '1':
            manager.create_card()
        elif choice == '2':
            manager.import_cards()
        elif choice == '3':
            manager.delete_card()
        elif choice == '4':
            manager.export_cards()
        elif choice == '5':
            manager.search_card()
        elif choice == '6':
            manager.list_all_cards()
        elif choice == '7':
            manager.show_element_table()
        elif choice == '8':
            manager.simulate_battle()
        elif choice == '0':
            print("感谢使用卡牌管理系统!")
            break
        else:
            print("无效的选择，请重新输入!")


if __name__ == "__main__":
    main()