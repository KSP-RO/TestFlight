import random
import matplotlib.pyplot as plt
from matplotlib import mlab


def roll():
	total = 0
	rolls = []
	for i in range(0,10):
		rolls.append(random.randint(1,20))
	# print "Base rolls ",sorted(rolls)
	sortedRolls = sorted(rolls)
	rolls = sortedRolls[:5]
	# print "Reduced Rolls ", sorted(rolls)
	final = rolls[0] + rolls[1] + rolls[2] + rolls[3] + rolls[4]
	# final = final - 5
	# print final
	return final

def generateRolls(count):
	allRolls = []
	for i in range(0, count):
		allRolls.append(roll())
	return allRolls

x = generateRolls(1000)
# print sorted(x)[0]
# print sorted(x)[1000/2]
# print sorted(x)[-1]

n_bins = 100
n, bins, patches = plt.hist(x, n_bins, normed=True, histtype='bar', cumulative=False)

# Add a line showing the expected distribution.
# y = mlab.normpdf(bins, mu, sigma).cumsum()
# y /= y[-1]
# plt.plot(bins, y, 'k--', linewidth=1.5)

# Overlay a reversed cumulative histogram.
# plt.hist(x, bins=bins, normed=1, histtype='step', cumulative=-1)

plt.grid(True)
# plt.xticks(range(0,20))
plt.ylim(0, 1)
plt.title('Failure Module Probability')

plt.show()