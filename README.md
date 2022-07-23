Edge Based Template Matching     
==============================

![click to preview](preview.gif)

### References   

[Edge Based Template Matching](https://www.codeproject.com/Articles/99457/Edge-Based-Template-Matching)    



### Different    

These differences can write less code and use opencv acceleration

#### 1 Use Canny directly
The reference article implements the Canny algorithm itself.      
Inserts the operation that creates the gradient template into it.    

The implementation here is different.    
First use Canny algorithm to find the edges.     
Then use FindContours find the edges to create a gradient template.    

#### 2 Use CartToPolar to get magnitude
Referencing the article by traversing the calculation magnitude.    

Here use the opencv's CartToPolar to get the magnitude and direction directly     



### Library   

Prism.Core   
OpenCvSharp4.Windows     
MaterialDesignThemes    
PropertyTools.Wpf

### Information collected on the network     

[Prof. Dr. Carsten Steger(Halcon)](https://iuks.in.tum.de/members/steger/publications)            
[Some video and publications(At the bottom)](http://campar.in.tum.de/Main/AndreasHofhauser)    
[shape_based_matching](https://github.com/meiqua/shape_based_matching)
