# DungeonGeneration
## Objectif
Générer un donjon procéduralement avec des pièces de même formes : avec une pièce de début, une pièce de boss, une pièce de fin et des salles de clés/bonus (à la Binding of Isaac).

Il faut également que le donjon généré soit découpé en plusieurs niveaux qui necessitent une clé pour y accéder (récupérable dans une salle de clé).

En s'inspirant du travail de Calvin Ashmore (https://smartech.gatech.edu/bitstream/handle/1853/16823/ashmore-thesis.pdf), on souhaite garder la trace de l'intensité de nos salles pour placer les récupération de clés dans des salles où la tension est forte.


On souhaite également générer les contenus des pièces procéduralement en utilisant une grammaire de génération.

Ces salles seront découpées en grilles (par défaut 5*5) et chaque case pourra avoir le type :
* Vide
* Ennemi
* Bonus
* Obstacle

Enfin on veut que toutes les case devant les portes soient vides, pour que le joueur ne soit pas bloqué ou tombe nez à nez avec un ennemi et on veut qu'il y ait toujours un chemin de libre pour accéder aux salles adjacentes.

Mon travail se découpe donc en 2 grandes partie : la génération de donjon et du contenu des salles.

*NB : Ce que j'ai fait se rapproche plus d'une API que d'un jeu fini, pour l'instant seule une vue de débug est faite sur Unity, mais l'idéal serait qu'un client qui récupère les données générées, prenne en compte l'intensité pour choisir les monstres à mettre dans les zones de monstres d'une salle par exemple*

## Génération du donjon
La génération du donjon se découpe en 6 étapes : 
1. Placer la première pièce
2. Placer les autres salles : 
    * On récupère aléatoirement un pièces avec des place de libre autour d'elle pour le niveau actuel du donjon
    * si aucune pièce n'est disponible pour le niveau actuel ou si on à déjà le nombre de salle voulu par niveau, on passe au niveau suivant
    * On créer aléatoirement une pièce adjacente sur une place libre
    * Lorsque l'on créer les liens entre les pièces on précise si l'accès est bloqué (si les deux salles sont d'un niveau différent)
3. Placer les salles de boss et de fin : 
    * On veut que notre salle de boss & de fin soit toujours à la fin du donjon, on incrémente donc le niveau de clé pour les placer
    * On récupère une feuille de notre arbre de rooms pour le dernier niveau (avant celui du boss) et on lui assigne le type de salle "Boss"
    * On créer la salle finale adjacente à la salle de boss
4. "Graphifier" notre arbre pour ajouter des chemins
    * On ajoute aléatoirement des liens entre nos salles
5. Calculer l'intensité des pièces : 
    * On parcourt récursivement le graph pour mettre une première version de l'intensité, le salle de départ à une intensité de 0 puis on ajoute aux rooms adjacente 1 d'intensité etc
    * On s'assure que la salle de Boss possède la plus forte intensité
    * On normalize le tout
6. Placer les salles de clés
    * On récupère la plus forte intensité d'un niveau et on lui assigne le type de salle "Key"


## Génération du contenu des salles
La génération des salles se découpe en 3 étapes simples :
1. Génération d'un simili graph
2. Appliquer les patterns de grammaires
3. Remplir les dernières cases de contenu par du vide

Pour s'assurer qu'il y ait toujours un chemin de libre, on utilise une grammaire de génération.

Les patterns de grammaire à appliquer possède des caractéristique comme un niveau permettant de s'assurer que certains sont appliqués avant d'autres.
Ainsi on peut avoir des patterns de "structure" qui s'assureront qu'il y a toujours un chemin.

Toute la difficulté d'avoir un donjon qui correpond à nos attentes est donc transposé dans ces patterns.

Par ailleurs on peut noter que l'on peut également appliquer certains patterns à seulement certains types de salles, cela permet un plus grand contrôle sur la génération si l'on souhaite que nos salles de clés ait une apparence différente.
