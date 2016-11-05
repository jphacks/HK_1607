RRails.application.routes.draw do
  resources :users
  get 'student' => 'students#index'

  # For details on the DSL available within this file, see http://guides.rubyonrails.org/routing.html
end
